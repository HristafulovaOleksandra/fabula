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
[Route("api/stories")]
public class StoriesController(FabulaDbContext db, EconomySettings economy) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StorySummaryDto>>> List(
        [FromQuery] CefrLevel? level,
        [FromQuery] int? tagId,
        [FromQuery] bool mine,
        [FromQuery] int? genreId,
        [FromQuery] int? topicId,
        [FromQuery] string? q,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var query = db.Stories.AsNoTracking().Include(s => s.Author).Include(s => s.Tags).Include(s => s.Scenes).AsQueryable();
        if (mine)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            var userId = User.UserId();
            query = query.Where(s => s.AuthorId == userId);
        }
        else
        {
            query = query.Where(s => s.Status == StoryStatus.Published);
        }

        if (level is not null)
        {
            query = query.Where(s => s.Level == level);
        }

        foreach (var id in new[] { tagId, genreId, topicId }.OfType<int>())
        {
            query = query.Where(s => s.Tags.Any(t => t.Id == id));
        }

        var search = q?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = LikePattern.Contains(search);
            var username = search.TrimStart('@').ToLowerInvariant();
            query = query.Where(s => EF.Functions.ILike(s.Title, pattern, LikePattern.Escape)
                                     || EF.Functions.ILike(s.Description, pattern, LikePattern.Escape)
                                     || s.Author!.Username == username);
        }

        query = sort == "rating"
            ? query.OrderByDescending(s => s.RatingCount == 0 ? 0 : (double)s.RatingSum / s.RatingCount).ThenByDescending(s => s.RatingCount).ThenByDescending(s => s.UpdatedAt)
            : query.OrderByDescending(s => s.UpdatedAt);
        var stories = await query.AsSplitQuery().ToListAsync(cancellationToken);
        return Ok(stories.Select(s => s.ToSummary()).ToList());
    }

    [Authorize]
    [HttpGet("for-you")]
    public async Task<ActionResult<ForYouDto>> ForYou(CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId, cancellationToken);
        var playedIds = await db.GameSessions.AsNoTracking()
            .Where(g => g.UserId == userId && g.FinishedAt != null)
            .Select(g => g.StoryId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var played = await db.Stories.AsNoTracking().Include(s => s.Tags)
            .Where(s => playedIds.Contains(s.Id))
            .ToListAsync(cancellationToken);
        var level = user.Level ?? played.GroupBy(s => s.Level).OrderByDescending(g => g.Count()).Select(g => (CefrLevel?)g.Key).FirstOrDefault();
        var favorite = user.FavoriteTopics.Select(t => t.ToLowerInvariant()).ToHashSet();
        var playedTags = played.SelectMany(s => s.Tags).Select(t => t.Id).ToHashSet();
        var followed = (await db.Follows.AsNoTracking().Where(f => f.FollowerId == userId).Select(f => f.AuthorId).ToListAsync(cancellationToken)).ToHashSet();

        var query = db.Stories.AsNoTracking()
            .Include(s => s.Author).Include(s => s.Tags).Include(s => s.Scenes)
            .Where(s => s.Status == StoryStatus.Published && s.AuthorId != userId && !playedIds.Contains(s.Id));
        if (level is not null)
        {
            query = query.Where(s => s.Level == level);
        }

        var candidates = await query.AsSplitQuery().ToListAsync(cancellationToken);
        var picks = candidates
            .Select(story =>
            {
                var favoriteMatches = story.Tags.Where(t => favorite.Contains(t.Name.ToLowerInvariant())).Select(t => t.Name).ToList();
                var playedMatches = story.Tags.Where(t => playedTags.Contains(t.Id) && !favoriteMatches.Contains(t.Name)).Select(t => t.Name).ToList();
                var fromFollowed = followed.Contains(story.AuthorId);
                var reasons = new List<string>();
                if (level is not null)
                {
                    reasons.Add($"Твій рівень {level}");
                }

                if (favoriteMatches.Count > 0)
                {
                    reasons.Add($"Улюблені теми: {string.Join(", ", favoriteMatches)}");
                }

                if (playedMatches.Count > 0)
                {
                    reasons.Add($"Схоже на прочитане: {string.Join(", ", playedMatches.Take(2))}");
                }

                if (fromFollowed)
                {
                    reasons.Add("Автор, на якого ти підписаний(-а)");
                }

                var score = 2 * favoriteMatches.Count + playedMatches.Count + (fromFollowed ? 2 : 0);
                return (story, score, reasons);
            })
            .OrderByDescending(p => p.score)
            .ThenByDescending(p => p.story.RatingCount == 0 ? 0 : (double)p.story.RatingSum / p.story.RatingCount)
            .ThenByDescending(p => p.story.UpdatedAt)
            .Take(8)
            .Select(p => new ForYouStoryDto(p.story.ToSummary(), p.reasons))
            .ToList();
        return Ok(new ForYouDto(level, user.Level is not null, picks));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StoryDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var story = await StoryRepository.LoadFullAsync(db, id, cancellationToken);
        if (story is null || (story.Status != StoryStatus.Published && !CanEdit(story)))
        {
            return NotFound();
        }

        return Ok(story.ToDetail(forEditor: CanEdit(story)));
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpPost]
    public async Task<ActionResult<StoryDetailDto>> Create(StoryUpsertRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var story = new Story
        {
            AuthorId = User.UserId(),
            Title = request.Title.Trim(),
            Status = StoryStatus.Draft,
            CreatedAt = now
        };
        var problem = await ApplyAsync(story, request, cancellationToken);
        if (problem is not null)
        {
            return problem;
        }

        story.UpdatedAt = now;
        db.Stories.Add(story);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = story.Id }, story.ToDetail(forEditor: true));
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StoryDetailDto>> Update(Guid id, StoryUpsertRequest request, CancellationToken cancellationToken)
    {
        var story = await StoryRepository.LoadFullAsync(db, id, cancellationToken, tracking: true);
        if (story is null || !CanEdit(story))
        {
            return NotFound();
        }

        var problem = await ApplyAsync(story, request, cancellationToken);
        if (problem is not null)
        {
            return problem;
        }

        story.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(story.ToDetail(forEditor: CanEdit(story)));
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<StoryDetailDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        var story = await StoryRepository.LoadFullAsync(db, id, cancellationToken, tracking: true);
        if (story is null || !CanEdit(story))
        {
            return NotFound();
        }

        if (story.Scenes.Count == 0 || story.StartSceneId is null)
        {
            return ValidationProblem("Історію без сцен або без початкової сцени не можна опублікувати.");
        }

        story.Status = StoryStatus.Published;
        story.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(story.ToDetail(forEditor: CanEdit(story)));
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var story = await db.Stories.FindAsync([id], cancellationToken);
        if (story is null || !CanEdit(story))
        {
            return NotFound();
        }

        db.Stories.Remove(story);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpPost("{id:guid}/allowed-words")]
    public async Task<ActionResult<IReadOnlyList<string>>> AllowWord(Guid id, AllowedWordRequest request, CancellationToken cancellationToken)
    {
        var story = await db.Stories.FindAsync([id], cancellationToken);
        if (story is null || !CanEdit(story))
        {
            return NotFound();
        }

        var word = request.Word.Trim().ToLowerInvariant();
        if (!story.AllowedWords.Contains(word))
        {
            if (story.AllowedWords.Count >= MaxAllowedWords)
            {
                return ValidationProblem($"У списку дозволених слів може бути до {MaxAllowedWords} слів.");
            }

            story.AllowedWords = [.. story.AllowedWords, word];
            story.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(story.AllowedWords.Order().ToList());
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpDelete("{id:guid}/allowed-words/{word}")]
    public async Task<ActionResult<IReadOnlyList<string>>> DisallowWord(Guid id, string word, CancellationToken cancellationToken)
    {
        var story = await db.Stories.FindAsync([id], cancellationToken);
        if (story is null || !CanEdit(story))
        {
            return NotFound();
        }

        story.AllowedWords = story.AllowedWords.Where(w => w != word.Trim().ToLowerInvariant()).ToList();
        story.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(story.AllowedWords.Order().ToList());
    }

    private const int MaxAllowedWords = 200;

    private bool CanEdit(Story story) =>
        User.Identity?.IsAuthenticated == true && (story.AuthorId == User.UserId() || User.IsAdmin());

    private async Task<ActionResult?> ApplyAsync(Story story, StoryUpsertRequest request, CancellationToken cancellationToken)
    {
        if (request.StartSceneId is not null && story.Scenes.All(s => s.Id != request.StartSceneId))
        {
            return ValidationProblem("Початкова сцена має належати цій історії.");
        }

        if (!economy.IsValidStoryPrice(request.PriceCrystals))
        {
            return ValidationProblem(economy.StoryPriceRule);
        }

        var tagIds = request.TagIds?.Distinct().ToList() ?? [];
        var tags = await db.Tags
            .Where(t => tagIds.Contains(t.Id) && t.Status != ModerationStatus.Rejected)
            .ToListAsync(cancellationToken);
        if (tags.Count != tagIds.Count)
        {
            return ValidationProblem("Невідомий або відхилений тег.");
        }

        story.Title = request.Title.Trim();
        story.Description = request.Description?.Trim() ?? "";
        story.CoverImagePath = request.CoverImagePath;
        story.CoverFocusX = request.CoverFocusX;
        story.CoverFocusY = request.CoverFocusY;
        story.CoverZoom = request.CoverZoom;
        story.Level = request.Level;
        story.PriceCrystals = request.PriceCrystals;
        story.StartSceneId = request.StartSceneId ?? story.StartSceneId;
        story.Tags.Clear();
        story.Tags.AddRange(tags);
        db.StoryWords.RemoveRange(story.Words);
        story.Words.Clear();
        story.Words.AddRange((request.Words ?? [])
            .DistinctBy(w => w.Word.Trim().ToLowerInvariant())
            .Select(w => new StoryWord { Word = w.Word.Trim(), Translation = w.Translation.Trim() }));
        return null;
    }
}
