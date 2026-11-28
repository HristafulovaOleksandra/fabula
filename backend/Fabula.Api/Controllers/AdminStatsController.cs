using Fabula.Api.Auth;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Fabula.Api.Ml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

public record NamedCount(string Name, int Count);

public record StoryMlRow(
    Guid Id,
    string Title,
    string AuthorUsername,
    CefrLevel Level,
    StoryStatus Status,
    AgeRating AgeRating,
    int TotalLines,
    int RatedLines,
    int ShortLines,
    int AboveLevel);

public record MlStatsDto(
    string? ModelName,
    int TotalLines,
    int RatedLines,
    int ShortLines,
    int UncheckedLines,
    IReadOnlyList<NamedCount> Levels,
    IReadOnlyList<NamedCount> CheckedPerDay,
    IReadOnlyList<NamedCount> StoryAges,
    IReadOnlyList<NamedCount> StoryStatuses,
    IReadOnlyList<NamedCount> ImageVerdicts,
    int AdminApproved,
    int AdminRejected,
    int AutoResolved,
    int OpenItems,
    IReadOnlyList<StoryMlRow> Stories);

public record LineMlDto(
    Guid Id,
    string SceneTitle,
    int SceneOrder,
    int Order,
    string Speaker,
    string Text,
    CefrLevel? Level,
    double? Score,
    DateTime? CheckedAt);

public record StoryMlDetailDto(StoryMlRow Story, IReadOnlyList<LineMlDto> Lines);

[ApiController]
[Authorize(Policy = Policies.AdminOnly)]
[Route("api/admin/ml-stats")]
public class AdminStatsController(FabulaDbContext db, MlClient ml) : ControllerBase
{
    private const int Days = 14;

    [HttpGet]
    public async Task<ActionResult<MlStatsDto>> Get(CancellationToken cancellationToken)
    {
        var lines = await db.Lines.AsNoTracking()
            .Join(db.Scenes, line => line.SceneId, scene => scene.Id, (line, scene) => new { scene.StoryId, line.Level, line.LevelCheckedAt })
            .ToListAsync(cancellationToken);
        var stories = await db.Stories.AsNoTracking().Include(s => s.Author)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);
        var since = DateTime.UtcNow.Date.AddDays(-(Days - 1));
        var perDay = lines.Where(l => l.LevelCheckedAt >= since)
            .GroupBy(l => l.LevelCheckedAt!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());
        var items = await db.ModerationItems.AsNoTracking()
            .Where(m => m.TargetType == ModerationTarget.Story || m.TargetType == ModerationTarget.Tag)
            .Select(m => new { m.Status, m.ReviewedById })
            .ToListAsync(cancellationToken);
        var images = await db.ImageChecks.AsNoTracking().GroupBy(c => c.Verdict)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var rows = stories.Select(story =>
        {
            var own = lines.Where(l => l.StoryId == story.Id).ToList();
            return new StoryMlRow(
                story.Id, story.Title, story.Author?.Username ?? "", story.Level, story.Status, story.AgeRating,
                own.Count,
                own.Count(l => l.Level != null),
                own.Count(l => l.Level == null && l.LevelCheckedAt != null),
                own.Count(l => l.Level != null && l.Level > story.Level));
        }).ToList();

        return Ok(new MlStatsDto(
            await ml.ModelNameAsync(cancellationToken),
            lines.Count,
            lines.Count(l => l.Level != null),
            lines.Count(l => l.Level == null && l.LevelCheckedAt != null),
            lines.Count(l => l.LevelCheckedAt == null),
            Enum.GetValues<CefrLevel>().Select(level => new NamedCount(level.ToString(), lines.Count(l => l.Level == level))).ToList(),
            Enumerable.Range(0, Days).Select(offset => since.AddDays(offset))
                .Select(day => new NamedCount(day.ToString("yyyy-MM-dd"), perDay.GetValueOrDefault(day))).ToList(),
            Enum.GetValues<AgeRating>().Select(age => new NamedCount(age.ToString(),
                stories.Count(s => s.AgeRating == age && s.Status != StoryStatus.Draft))).ToList(),
            Enum.GetValues<StoryStatus>().Select(status => new NamedCount(status.ToString(), stories.Count(s => s.Status == status))).ToList(),
            Enum.GetValues<ModerationVerdict>().Select(verdict => new NamedCount(verdict.ToString(),
                images.Where(i => i.Key == verdict).Sum(i => i.Count))).ToList(),
            items.Count(i => i.Status == ModerationStatus.Approved && i.ReviewedById != null),
            items.Count(i => i.Status == ModerationStatus.Rejected),
            items.Count(i => i.Status == ModerationStatus.Approved && i.ReviewedById == null),
            items.Count(i => i.Status is ModerationStatus.Pending or ModerationStatus.Appealed),
            rows));
    }

    [HttpGet("stories/{id:guid}")]
    public async Task<ActionResult<StoryMlDetailDto>> Story(Guid id, CancellationToken cancellationToken)
    {
        var story = await db.Stories.AsNoTracking().Include(s => s.Author).SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (story is null)
        {
            return NotFound();
        }

        var lines = await db.Lines.AsNoTracking()
            .Join(db.Scenes, line => line.SceneId, scene => scene.Id, (line, scene) => new { line, scene })
            .Where(x => x.scene.StoryId == id)
            .OrderBy(x => x.scene.Order).ThenBy(x => x.line.Order)
            .Select(x => new LineMlDto(x.line.Id, x.scene.Title, x.scene.Order, x.line.Order, x.line.Speaker, x.line.Text,
                x.line.Level, x.line.LevelScore, x.line.LevelCheckedAt))
            .ToListAsync(cancellationToken);
        var row = new StoryMlRow(story.Id, story.Title, story.Author?.Username ?? "", story.Level, story.Status, story.AgeRating,
            lines.Count, lines.Count(l => l.Level != null), lines.Count(l => l.Level == null && l.CheckedAt != null),
            lines.Count(l => l.Level != null && l.Level > story.Level));
        return Ok(new StoryMlDetailDto(row, lines));
    }
}
