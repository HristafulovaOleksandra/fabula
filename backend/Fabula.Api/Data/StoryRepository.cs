using Fabula.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Data;

public static class StoryRepository
{
    public static Task<Story?> LoadFullAsync(FabulaDbContext db, Guid id, CancellationToken cancellationToken, bool tracking = false)
    {
        IQueryable<Story> query = db.Stories
            .Include(s => s.Author)
            .Include(s => s.Tags)
            .Include(s => s.Words)
            .Include(s => s.Cast).ThenInclude(c => c.States)
            .Include(s => s.Scenes).ThenInclude(s => s.Background)
            .Include(s => s.Scenes).ThenInclude(s => s.Characters).ThenInclude(c => c.StoryCharacter).ThenInclude(c => c!.States)
            .Include(s => s.Scenes).ThenInclude(s => s.Lines)
            .Include(s => s.Scenes).ThenInclude(s => s.Choices)
            .AsSplitQuery();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
