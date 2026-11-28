using Fabula.Api.Data;
using Fabula.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Moderation;

public class PublishedStoriesCheck(IServiceScopeFactory scopes, ILogger<PublishedStoriesCheck> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(20);
    private const int MaxAttempts = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 0; attempt < MaxAttempts && !stoppingToken.IsCancellationRequested; attempt++)
        {
            await Task.Delay(RetryDelay, stoppingToken);
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FabulaDbContext>();
            var moderation = scope.ServiceProvider.GetRequiredService<StoryModeration>();
            var ids = await db.Stories
                .Where(s => s.Status == StoryStatus.Published || s.Status == StoryStatus.PendingModeration)
                .Select(s => s.Id)
                .ToListAsync(stoppingToken);
            var done = true;
            foreach (var id in ids)
            {
                var report = await moderation.ApplyAsync(id, publishing: false, stoppingToken);
                if (report is { Available: false })
                {
                    done = false;
                    break;
                }
            }

            if (done)
            {
                logger.LogInformation("Перевірено опубліковані історії: {Count}", ids.Count);
                return;
            }
        }
    }
}
