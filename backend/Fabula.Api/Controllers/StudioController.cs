using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.CanAuthor)]
[Route("api/studio")]
public class StudioController(FabulaDbContext db) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<StudioOverviewDto>> Overview(CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var recent = await db.Stories.AsNoTracking()
            .Include(s => s.Author).Include(s => s.Tags).Include(s => s.Scenes)
            .Where(s => s.AuthorId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(4)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        var ids = recent.Select(s => s.Id).ToList();
        var lines = await db.Lines.AsNoTracking()
            .Join(db.Scenes, line => line.SceneId, scene => scene.Id, (line, scene) => new { scene.StoryId, line.Level, line.LevelCheckedAt })
            .Where(l => ids.Contains(l.StoryId))
            .ToListAsync(cancellationToken);

        var stories = recent.Select(story =>
        {
            var own = lines.Where(l => l.StoryId == story.Id).ToList();
            return new StudioStoryDto(
                story.ToSummary(),
                own.Count,
                own.Count(l => l.LevelCheckedAt is null),
                own.Count(l => l.Level is not null && l.Level > story.Level));
        }).ToList();

        var counts = await db.Stories.AsNoTracking()
            .Where(s => s.AuthorId == userId)
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var links = await db.TeachingLinks.AsNoTracking()
            .Where(l => l.TeacherId == userId)
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var followers = await db.Follows.CountAsync(f => f.AuthorId == userId, cancellationToken);

        return Ok(new StudioOverviewDto(
            stories,
            counts.Sum(c => c.Count),
            counts.Where(c => c.Status == StoryStatus.Published).Sum(c => c.Count),
            counts.Where(c => c.Status == StoryStatus.Draft).Sum(c => c.Count),
            links.Where(l => l.Status == TeachingStatus.Accepted).Sum(l => l.Count),
            links.Where(l => l.Status == TeachingStatus.Pending).Sum(l => l.Count),
            followers));
    }
}
