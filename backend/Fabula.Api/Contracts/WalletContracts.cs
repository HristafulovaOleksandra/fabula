using Fabula.Api.Economy;
using Fabula.Api.Entities;

namespace Fabula.Api.Contracts;

public record StreakDto(int Current, int Longest, bool PlayedToday, int RescueDays, int RescuePrice);

public enum StreakDayState
{
    Done,
    Rescued,
    Missed,
    Today,
    Future
}

public record StreakDayDto(DateOnly Date, StreakDayState State, bool IsToday);

public record WalletDto(int Balance, StreakDto Streak, int DailyGoalReward, IReadOnlyList<StreakDayDto> Week);

public record RecentStoryDto(StorySummaryDto Story, DateTime LastPlayedAt, bool Completed);

public record TransactionDto(Guid Id, int Amount, TransactionReason Reason, string? StoryTitle, DateTime CreatedAt);

public record RescueResultDto(WalletDto Wallet, IReadOnlyList<Reward> Rewards);

public record StoryAccessDto(int Price, bool Owned, bool Free, bool Completed, int Reward, int Balance);
