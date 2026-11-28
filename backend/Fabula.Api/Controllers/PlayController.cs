using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Economy;
using Fabula.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class PlayController(FabulaDbContext db, WalletService wallet, EconomySettings economy) : ControllerBase
{
    [HttpGet("play/recent")]
    public async Task<ActionResult<IReadOnlyList<RecentStoryDto>>> Recent(CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var played = await db.GameSessions.AsNoTracking()
            .Where(g => g.UserId == userId)
            .GroupBy(g => g.StoryId)
            .Select(g => new { StoryId = g.Key, LastPlayedAt = g.Max(x => x.StartedAt) })
            .OrderByDescending(g => g.LastPlayedAt)
            .Take(6)
            .ToListAsync(cancellationToken);
        var ids = played.Select(p => p.StoryId).ToList();
        var stories = await db.Stories.AsNoTracking()
            .Include(s => s.Author).Include(s => s.Tags).Include(s => s.Scenes)
            .Where(s => ids.Contains(s.Id) && (s.Status == StoryStatus.Published || s.AuthorId == userId))
            .AsSplitQuery()
            .ToDictionaryAsync(s => s.Id, cancellationToken);
        var completed = await db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Reason == TransactionReason.StoryCompleted && t.StoryId != null && ids.Contains(t.StoryId.Value))
            .Select(t => t.StoryId!.Value)
            .ToListAsync(cancellationToken);
        return Ok(played
            .Where(p => stories.ContainsKey(p.StoryId))
            .Select(p => new RecentStoryDto(stories[p.StoryId].ToSummary(), p.LastPlayedAt, completed.Contains(p.StoryId)))
            .ToList());
    }

    [HttpGet("play/{storyId:guid}/access")]
    public async Task<ActionResult<StoryAccessDto>> Access(Guid storyId, CancellationToken cancellationToken)
    {
        var story = await FindPlayableStoryAsync(storyId, cancellationToken);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == User.UserId(), cancellationToken);
        if (story is null || user is null)
        {
            return NotFound();
        }

        return Ok(await DescribeAccessAsync(story, user, cancellationToken));
    }

    [HttpPost("play/{storyId:guid}/purchase")]
    public async Task<ActionResult<StoryAccessDto>> Purchase(Guid storyId, CancellationToken cancellationToken)
    {
        var story = await FindPlayableStoryAsync(storyId, cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == User.UserId(), cancellationToken);
        if (story is null || user is null)
        {
            return NotFound();
        }

        if (!await OwnsStoryAsync(story, user.Id, cancellationToken)
            && !wallet.TrySpend(user, story.PriceCrystals, TransactionReason.PaidStory, story.Id))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, detail: WalletService.NotEnough(story.PriceCrystals, user.CrystalBalance));
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(await DescribeAccessAsync(story, user, cancellationToken));
    }

    [HttpPost("play/{storyId:guid}/start")]
    public async Task<ActionResult<PlaySessionDto>> Start(Guid storyId, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var story = await FindPlayableStoryAsync(storyId, cancellationToken);
        if (story is null)
        {
            return NotFound();
        }

        if (story.StartSceneId is null)
        {
            return ValidationProblem("В історії ще немає сцен.");
        }

        if (!await OwnsStoryAsync(story, userId, cancellationToken))
        {
            return Problem(statusCode: StatusCodes.Status402PaymentRequired,
                detail: $"Цю історію спершу треба придбати за {story.PriceCrystals} кристалів.");
        }

        var session = new GameSession
        {
            UserId = userId,
            StoryId = story.Id,
            StartedAt = DateTime.UtcNow,
            ScenePath = [story.StartSceneId.Value]
        };
        db.GameSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        var purchased = await db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.StoryId == story.Id && t.Reason == TransactionReason.PaidChoice && t.ChoiceId != null)
            .Select(t => t.ChoiceId!.Value)
            .ToListAsync(cancellationToken);
        return Ok(new PlaySessionDto(session.Id, story.Id, story.StartSceneId, IsFree(story), purchased));
    }

    [HttpPost("play/sessions/{sessionId:guid}/choice")]
    public async Task<ActionResult<ChoiceResultDto>> Choose(Guid sessionId, MakeChoiceRequest request, CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        if (session.FinishedAt is not null)
        {
            return ValidationProblem("Це проходження вже завершене.");
        }

        var currentSceneId = session.ScenePath.LastOrDefault();
        var choice = await db.Choices.AsNoTracking().SingleOrDefaultAsync(
            c => c.Id == request.ChoiceId && c.SceneId == currentSceneId, cancellationToken);
        if (choice is null)
        {
            return ValidationProblem("Цей вибір недоступний у поточній сцені.");
        }

        var story = await db.Stories.AsNoTracking().SingleAsync(s => s.Id == session.StoryId, cancellationToken);
        var user = await db.Users.SingleAsync(u => u.Id == session.UserId, cancellationToken);
        var spent = 0;
        if (choice.PriceCrystals > 0 && !IsFree(story))
        {
            var bought = await db.Transactions.AnyAsync(
                t => t.UserId == user.Id && t.ChoiceId == choice.Id && t.Reason == TransactionReason.PaidChoice, cancellationToken);
            if (!bought)
            {
                if (!wallet.TrySpend(user, choice.PriceCrystals, TransactionReason.PaidChoice, story.Id, choice.Id))
                {
                    return Problem(statusCode: StatusCodes.Status409Conflict, detail: WalletService.NotEnough(choice.PriceCrystals, user.CrystalBalance));
                }

                spent = choice.PriceCrystals;
            }
        }

        session.ChoiceIds = [.. session.ChoiceIds, choice.Id];
        if (choice.NextSceneId is not null)
        {
            session.ScenePath = [.. session.ScenePath, choice.NextSceneId.Value];
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ChoiceResultDto(choice.NextSceneId, choice.NextSceneId is null, spent, user.CrystalBalance));
    }

    [HttpPost("play/sessions/{sessionId:guid}/finish")]
    public async Task<ActionResult<PlaySummaryDto>> Finish(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        var story = await db.Stories.AsNoTracking().SingleAsync(s => s.Id == session.StoryId, cancellationToken);
        var user = await db.Users.SingleAsync(u => u.Id == session.UserId, cancellationToken);
        var streak = await wallet.LoadStreakAsync(user.Id, cancellationToken);
        var completed = await ReachedEndingAsync(session, cancellationToken);
        var rewards = new List<Reward>();
        if (session.FinishedAt is null)
        {
            session.FinishedAt = DateTime.UtcNow;
            if (completed && !IsFree(story))
            {
                var firstTime = !await db.Transactions.AnyAsync(
                    t => t.UserId == user.Id && t.StoryId == story.Id && t.Reason == TransactionReason.StoryCompleted, cancellationToken);
                var storyReward = economy.StoryReward(story.Level);
                if (firstTime && storyReward > 0)
                {
                    rewards.Add(wallet.Add(user, storyReward, TransactionReason.StoryCompleted, story.Id));
                }

                rewards.AddRange(wallet.RecordActivity(user, streak));
                session.CrystalsEarned = rewards.Sum(r => r.Amount);
            }

            if (completed)
            {
                await db.Assignments
                    .Where(a => a.StudentId == user.Id && a.StoryId == story.Id && a.CompletedAt == null)
                    .ExecuteUpdateAsync(update => update.SetProperty(a => a.CompletedAt, DateTime.UtcNow), cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var words = await db.UserWords.AsNoTracking()
            .Where(w => w.UserId == session.UserId && w.SourceStoryId == session.StoryId && w.LastSeenAt >= session.StartedAt)
            .OrderBy(w => w.SurfaceForm)
            .Select(w => new StoryWordDto(w.SurfaceForm, w.Translation))
            .ToListAsync(cancellationToken);
        return Ok(new PlaySummaryDto(
            session.ScenePath.Distinct().Count(),
            session.ChoiceIds.Count,
            words,
            session.CrystalsEarned,
            Math.Round((session.FinishedAt.Value - session.StartedAt).TotalMinutes, 1),
            completed,
            IsFree(story),
            rewards,
            user.CrystalBalance,
            wallet.DescribeStreak(streak),
            await CanRateAsync(story, user.Id, cancellationToken),
            await MyRatingAsync(story.Id, user.Id, cancellationToken)));
    }

    [HttpPut("stories/{storyId:guid}/rating")]
    public async Task<ActionResult<StoryRatingDto>> Rate(Guid storyId, RateStoryRequest request, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var story = await db.Stories.SingleOrDefaultAsync(s => s.Id == storyId && s.Status == StoryStatus.Published, cancellationToken);
        if (story is null)
        {
            return NotFound();
        }

        if (!await CanRateAsync(story, userId, cancellationToken))
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Оцінити історію можна після того, як пройдеш її до кінця.");
        }

        var now = DateTime.UtcNow;
        var rating = await db.StoryRatings.SingleOrDefaultAsync(r => r.UserId == userId && r.StoryId == storyId, cancellationToken);
        if (rating is null)
        {
            db.StoryRatings.Add(new StoryRating { UserId = userId, StoryId = storyId, Stars = request.Stars, CreatedAt = now, UpdatedAt = now });
            story.RatingCount++;
            story.RatingSum += request.Stars;
        }
        else
        {
            story.RatingSum += request.Stars - rating.Stars;
            rating.Stars = request.Stars;
            rating.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new StoryRatingDto(request.Stars, Math.Round((double)story.RatingSum / story.RatingCount, 1), story.RatingCount));
    }

    private bool IsFree(Story story) =>
        story.Status != StoryStatus.Published || story.AuthorId == User.UserId() || User.IsAdmin();

    private async Task<Story?> FindPlayableStoryAsync(Guid storyId, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var story = await db.Stories.AsNoTracking().SingleOrDefaultAsync(s => s.Id == storyId, cancellationToken);
        var canPreview = story is not null && (story.AuthorId == userId || User.IsAdmin());
        return story is null || (story.Status != StoryStatus.Published && !canPreview) ? null : story;
    }

    private async Task<bool> OwnsStoryAsync(Story story, Guid userId, CancellationToken cancellationToken) =>
        story.PriceCrystals == 0
        || IsFree(story)
        || await db.Assignments.AnyAsync(a => a.StudentId == userId && a.StoryId == story.Id, cancellationToken)
        || await db.Transactions.AnyAsync(
            t => t.UserId == userId && t.StoryId == story.Id && t.Reason == TransactionReason.PaidStory, cancellationToken);

    private async Task<StoryAccessDto> DescribeAccessAsync(Story story, User user, CancellationToken cancellationToken)
    {
        var free = IsFree(story);
        var completed = await db.Transactions.AnyAsync(
            t => t.UserId == user.Id && t.StoryId == story.Id && t.Reason == TransactionReason.StoryCompleted, cancellationToken);
        return new StoryAccessDto(
            story.PriceCrystals,
            await OwnsStoryAsync(story, user.Id, cancellationToken),
            free,
            completed,
            free || completed ? 0 : economy.StoryReward(story.Level),
            user.CrystalBalance,
            await CanRateAsync(story, user.Id, cancellationToken),
            await MyRatingAsync(story.Id, user.Id, cancellationToken),
            await db.Assignments.AsNoTracking()
                .Where(a => a.StudentId == user.Id && a.StoryId == story.Id)
                .Join(db.Users, a => a.TeacherId, u => u.Id, (a, u) => new AssignmentInfoDto(u.Username, a.DueDate, a.Note, a.CompletedAt != null))
                .SingleOrDefaultAsync(cancellationToken));
    }

    private async Task<bool> CanRateAsync(Story story, Guid userId, CancellationToken cancellationToken) =>
        story.Status == StoryStatus.Published
        && story.AuthorId != userId
        && await db.GameSessions.AnyAsync(g => g.UserId == userId && g.StoryId == story.Id && g.FinishedAt != null, cancellationToken);

    private Task<int?> MyRatingAsync(Guid storyId, Guid userId, CancellationToken cancellationToken) =>
        db.StoryRatings.Where(r => r.UserId == userId && r.StoryId == storyId).Select(r => (int?)r.Stars).SingleOrDefaultAsync(cancellationToken);

    private async Task<bool> ReachedEndingAsync(GameSession session, CancellationToken cancellationToken)
    {
        if (session.ChoiceIds.Count > 0)
        {
            var lastChoiceId = session.ChoiceIds[^1];
            if (await db.Choices.AnyAsync(c => c.Id == lastChoiceId && c.NextSceneId == null, cancellationToken))
            {
                return true;
            }
        }

        var lastSceneId = session.ScenePath.LastOrDefault();
        return !await db.Choices.AnyAsync(c => c.SceneId == lastSceneId, cancellationToken);
    }

    private Task<GameSession?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        return db.GameSessions.SingleOrDefaultAsync(g => g.Id == sessionId && g.UserId == userId, cancellationToken);
    }
}
