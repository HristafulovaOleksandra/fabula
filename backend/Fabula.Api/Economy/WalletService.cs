using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Economy;

public record Reward(TransactionReason Reason, int Amount);

public class WalletService(FabulaDbContext db, EconomySettings economy)
{
    private readonly TimeZoneInfo zone = TimeZoneInfo.TryFindSystemTimeZoneById(economy.TimeZone, out var found) ? found : TimeZoneInfo.Utc;

    public DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));

    public Reward Add(User user, int amount, TransactionReason reason, Guid? storyId = null, Guid? choiceId = null)
    {
        user.CrystalBalance += amount;
        db.Transactions.Add(new Transaction
        {
            UserId = user.Id,
            Amount = amount,
            Reason = reason,
            StoryId = storyId,
            ChoiceId = choiceId,
            CreatedAt = DateTime.UtcNow
        });
        return new Reward(reason, amount);
    }

    public bool TrySpend(User user, int price, TransactionReason reason, Guid? storyId = null, Guid? choiceId = null)
    {
        if (user.CrystalBalance < price)
        {
            return false;
        }

        Add(user, -price, reason, storyId, choiceId);
        return true;
    }

    public static string NotEnough(int price, int balance) =>
        $"Не вистачає кристалів: потрібно {price}, а в тебе {balance}.";

    public async Task<Streak> LoadStreakAsync(Guid userId, CancellationToken cancellationToken)
    {
        var streak = await db.Streaks.SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (streak is null)
        {
            streak = new Streak { UserId = userId };
            db.Streaks.Add(streak);
        }

        Refresh(streak, Today());
        return streak;
    }

    public static void Refresh(Streak streak, DateOnly today)
    {
        if (streak.RescuableUntil is not null && streak.RescuableUntil < today)
        {
            streak.RescuableDays = 0;
            streak.RescuableUntil = null;
        }

        if (streak.LastActiveDate is null || streak.CurrentDays == 0)
        {
            return;
        }

        var missedDays = today.DayNumber - streak.LastActiveDate.Value.DayNumber - 1;
        if (missedDays < 1)
        {
            return;
        }

        if (missedDays == 1)
        {
            streak.RescuableDays = streak.CurrentDays;
            streak.RescuableUntil = today;
        }

        streak.CurrentDays = 0;
    }

    public List<Reward> RecordActivity(User user, Streak streak)
    {
        var today = Today();
        if (streak.LastActiveDate == today)
        {
            return [];
        }

        var before = streak.CurrentDays;
        streak.CurrentDays++;
        streak.LastActiveDate = today;
        streak.LongestDays = Math.Max(streak.LongestDays, streak.CurrentDays);
        var rewards = new List<Reward> { Add(user, economy.DailyGoalReward, TransactionReason.DailyGoal) };
        rewards.AddRange(StreakBonuses(user, before, streak.CurrentDays));
        return rewards;
    }

    public bool CanRescue(Streak streak) => streak.RescuableUntil == Today() && streak.RescuableDays > 0;

    public StreakDto DescribeStreak(Streak streak) => new(
        streak.CurrentDays,
        streak.LongestDays,
        streak.LastActiveDate == Today(),
        CanRescue(streak) ? streak.RescuableDays : 0,
        economy.StreakFreezePrice);

    public async Task<WalletDto> DescribeAsync(User user, Streak streak, CancellationToken cancellationToken) =>
        new(user.CrystalBalance, DescribeStreak(streak), economy.DailyGoalReward, await WeekAsync(user.Id, cancellationToken));

    public async Task<IReadOnlyList<StreakDayDto>> WeekAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = Today();
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var since = monday.AddDays(-2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var events = await db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.CreatedAt >= since
                        && (t.Reason == TransactionReason.DailyGoal || t.Reason == TransactionReason.StreakFreeze))
            .Select(t => new { t.Reason, t.CreatedAt })
            .ToListAsync(cancellationToken);
        var done = events.Where(e => e.Reason == TransactionReason.DailyGoal).Select(e => LocalDate(e.CreatedAt)).ToHashSet();
        var rescued = events.Where(e => e.Reason == TransactionReason.StreakFreeze).Select(e => LocalDate(e.CreatedAt).AddDays(-1)).ToHashSet();
        return Enumerable.Range(0, 7)
            .Select(offset => monday.AddDays(offset))
            .Select(day => new StreakDayDto(day, done.Contains(day) ? StreakDayState.Done
                : rescued.Contains(day) ? StreakDayState.Rescued
                : day == today ? StreakDayState.Today
                : day > today ? StreakDayState.Future
                : StreakDayState.Missed, day == today))
            .ToList();
    }

    private DateOnly LocalDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone));

    public List<Reward> Rescue(User user, Streak streak)
    {
        var today = Today();
        var before = streak.CurrentDays;
        Add(user, -economy.StreakFreezePrice, TransactionReason.StreakFreeze);
        streak.CurrentDays = streak.RescuableDays + streak.CurrentDays;
        if (streak.LastActiveDate != today)
        {
            streak.LastActiveDate = today.AddDays(-1);
        }

        streak.RescuableDays = 0;
        streak.RescuableUntil = null;
        streak.LongestDays = Math.Max(streak.LongestDays, streak.CurrentDays);
        return StreakBonuses(user, before, streak.CurrentDays);
    }

    private List<Reward> StreakBonuses(User user, int before, int after) =>
        economy.StreakBonuses
            .Where(bonus => bonus.Key > before && bonus.Key <= after)
            .OrderBy(bonus => bonus.Key)
            .Select(bonus => Add(user, bonus.Value, TransactionReason.StreakBonus))
            .ToList();
}
