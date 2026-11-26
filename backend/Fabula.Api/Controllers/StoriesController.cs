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
        [FromQuery] CefrLevel? level, [FromQuery] int? tagId, [FromQuery] bool mine, CancellationToken cancellationToken)
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

        if (tagId is not null)
        {
            query = query.Where(s => s.Tags.Any(t => t.Id == tagId));
        }

        var stories = await query.OrderByDescending(s => s.UpdatedAt).AsSplitQuery().ToListAsync(cancellationToken);
        return Ok(stories.Select(s => s.ToSummary()).ToList());
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
