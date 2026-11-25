using Fabula.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Data;

public static class TeachingQueries
{
    public static Task<bool> IsTeacherAsync(this FabulaDbContext db, Guid userId, CancellationToken cancellationToken) =>
        db.TeachingLinks.AnyAsync(l => l.TeacherId == userId && l.Status == TeachingStatus.Accepted, cancellationToken);

    public static async Task<HashSet<Guid>> TeacherIdsAsync(this FabulaDbContext db, IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        var teachers = await db.TeachingLinks
            .Where(l => ids.Contains(l.TeacherId) && l.Status == TeachingStatus.Accepted)
            .Select(l => l.TeacherId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return teachers.ToHashSet();
    }
}
