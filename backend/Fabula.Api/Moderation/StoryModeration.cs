using Fabula.Api.Data;
using Fabula.Api.Entities;
using Fabula.Api.Ml;
using Fabula.Api.Storage;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Moderation;

public record ModerationFinding(
    string Kind,
    string Location,
    Guid? SceneId,
    string? Text,
    string? ImagePath,
    AgeRating Age,
    ModerationVerdict Verdict,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<MlFlaggedWord> Words);

public record ModerationReport(bool Available, AgeRating Age, ModerationVerdict Verdict, IReadOnlyList<ModerationFinding> Findings);

public class StoryModeration(FabulaDbContext db, MlClient ml, ImageStorage images)
{
    private record TextItem(string Location, Guid? SceneId, string Text);

    private record ImageItem(string Location, Guid? SceneId, string Path);

    public static AgeRating ParseAge(string age) => age switch
    {
        "16+" => AgeRating.Mature,
        "12+" => AgeRating.Teen,
        _ => AgeRating.All
    };

    public static ModerationVerdict ParseVerdict(string verdict) => verdict switch
    {
        "blocked" => ModerationVerdict.Blocked,
        "review" => ModerationVerdict.Review,
        _ => ModerationVerdict.Ok
    };

    public async Task<ModerationReport> CheckAsync(Story story, CancellationToken cancellationToken)
    {
        var texts = CollectTexts(story);
        var moderated = await ml.ModerateTextsAsync(texts.Select(t => t.Text).ToList(), cancellationToken);
        if (moderated is null)
        {
            return new ModerationReport(false, story.AgeRating, ModerationVerdict.Ok, []);
        }

        var approved = story.ApprovedContent.ToHashSet();
        var findings = new List<ModerationFinding>();
        foreach (var (item, result) in texts.Zip(moderated))
        {
            var age = ParseAge(result.Age);
            var verdict = approved.Contains(item.Text) ? ModerationVerdict.Ok : ParseVerdict(result.Verdict);
            if (age != AgeRating.All || verdict != ModerationVerdict.Ok)
            {
                findings.Add(new ModerationFinding("text", item.Location, item.SceneId, item.Text, null, age, verdict, result.Reasons, result.Words));
            }
        }

        foreach (var item in CollectImages(story))
        {
            var check = await CheckImageAsync(item.Path, cancellationToken);
            if (check is null)
            {
                return new ModerationReport(false, story.AgeRating, ModerationVerdict.Ok, []);
            }

            var imageVerdict = approved.Contains(item.Path) ? ModerationVerdict.Ok : check.Verdict;
            if (check.Age != AgeRating.All || imageVerdict != ModerationVerdict.Ok)
            {
                findings.Add(new ModerationFinding("image", item.Location, item.SceneId, null, item.Path, check.Age, imageVerdict, check.Reasons, []));
            }
        }

        var storyAge = findings.Count == 0 ? AgeRating.All : findings.Max(f => f.Age);
        var storyVerdict = findings.Count == 0 ? ModerationVerdict.Ok : findings.Max(f => f.Verdict);
        var ordered = findings
            .OrderByDescending(f => f.Verdict)
            .ThenByDescending(f => f.Age)
            .ToList();
        return new ModerationReport(true, storyAge, storyVerdict, ordered);
    }

    public async Task<ImageCheck?> CheckImageAsync(string publicPath, CancellationToken cancellationToken)
    {
        var cached = await db.ImageChecks.SingleOrDefaultAsync(c => c.Path == publicPath, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var local = images.LocalPath(publicPath);
        if (local is null)
        {
            return new ImageCheck { Path = publicPath, Verdict = ModerationVerdict.Ok, Age = AgeRating.All, CheckedAt = DateTime.UtcNow };
        }

        var result = await ml.ModerateImageAsync(await File.ReadAllBytesAsync(local, cancellationToken), cancellationToken);
        if (result is null)
        {
            return null;
        }

        var check = new ImageCheck
        {
            Path = publicPath,
            Verdict = ParseVerdict(result.Verdict),
            Age = ParseAge(result.Age),
            Reasons = result.Reasons.ToList(),
            CheckedAt = DateTime.UtcNow
        };
        db.ImageChecks.Add(check);
        await db.SaveChangesAsync(cancellationToken);
        return check;
    }

    public async Task<ModerationReport?> ApplyAsync(Guid storyId, bool publishing, CancellationToken cancellationToken)
    {
        var full = await StoryRepository.LoadFullAsync(db, storyId, cancellationToken);
        var story = await db.Stories.SingleOrDefaultAsync(s => s.Id == storyId, cancellationToken);
        if (full is null || story is null)
        {
            return null;
        }

        var report = await CheckAsync(full, cancellationToken);
        if (!report.Available)
        {
            return report;
        }

        story.AgeRating = report.Age;
        var summary = Summary(report);
        if (publishing)
        {
            story.Status = report.Verdict switch
            {
                ModerationVerdict.Blocked => StoryStatus.Blocked,
                ModerationVerdict.Review => StoryStatus.PendingModeration,
                _ => StoryStatus.Published
            };
            story.ModerationNote = summary;
        }
        else if (story.Status == StoryStatus.Published && report.Verdict != ModerationVerdict.Ok)
        {
            story.Status = StoryStatus.PendingModeration;
            story.ModerationNote = summary;
        }
        else if (story.Status == StoryStatus.PendingModeration && report.Verdict == ModerationVerdict.Ok)
        {
            story.Status = StoryStatus.Published;
            story.ModerationNote = "";
        }

        await SyncQueueAsync(story, summary, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return report;
    }

    private async Task SyncQueueAsync(Story story, string summary, CancellationToken cancellationToken)
    {
        var open = await db.ModerationItems
            .Where(m => m.TargetType == ModerationTarget.Story && m.TargetId == story.Id
                        && (m.Status == ModerationStatus.Pending || m.Status == ModerationStatus.Appealed))
            .ToListAsync(cancellationToken);
        if (story.Status == StoryStatus.PendingModeration)
        {
            var item = open.FirstOrDefault(m => m.Status == ModerationStatus.Pending);
            if (item is null)
            {
                db.ModerationItems.Add(new ModerationItem
                {
                    TargetType = ModerationTarget.Story,
                    TargetId = story.Id,
                    Reason = summary,
                    Status = ModerationStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                item.Reason = summary;
            }
        }
        else if (story.Status == StoryStatus.Published)
        {
            foreach (var item in open)
            {
                item.Status = ModerationStatus.Approved;
                item.ReviewedAt = DateTime.UtcNow;
                item.ResolutionNote = "Автор виправив історію, повторна перевірка пройдена автоматично.";
            }
        }
    }

    public static string Summary(ModerationReport report) =>
        string.Join("; ", report.Findings
            .Where(f => f.Verdict != ModerationVerdict.Ok)
            .Select(f => $"{f.Location}: {string.Join(", ", f.Reasons)}")
            .Distinct()
            .Take(10));

    private static List<TextItem> CollectTexts(Story story)
    {
        var items = new List<TextItem>
        {
            new("Назва історії", null, story.Title),
        };
        if (!string.IsNullOrWhiteSpace(story.Description))
        {
            items.Add(new TextItem("Опис історії", null, story.Description));
        }

        items.AddRange(story.Cast.Select(c => new TextItem($"Персонаж «{c.Name}»", null, c.Name)));
        foreach (var scene in story.Scenes.OrderBy(s => s.Order))
        {
            items.Add(new TextItem($"Сцена «{scene.Title}»: назва", scene.Id, scene.Title));
            items.AddRange(scene.Lines.OrderBy(l => l.Order).Select((line, index) =>
                new TextItem($"Сцена «{scene.Title}», репліка {index + 1}", scene.Id, line.Text)));
            items.AddRange(scene.Choices.OrderBy(c => c.Order).Select((choice, index) =>
                new TextItem($"Сцена «{scene.Title}», вибір {index + 1}", scene.Id, choice.Text)));
        }

        return items.Where(i => !string.IsNullOrWhiteSpace(i.Text)).ToList();
    }

    private static List<ImageItem> CollectImages(Story story)
    {
        var items = new List<ImageItem>();
        if (story.CoverImagePath is not null)
        {
            items.Add(new ImageItem("Обкладинка", null, story.CoverImagePath));
        }

        foreach (var character in story.Cast)
        {
            items.AddRange(character.States.Where(s => s.ImagePath is not null)
                .Select(s => new ImageItem($"Персонаж «{character.Name}», стан «{s.Key}»", null, s.ImagePath!)));
        }

        foreach (var scene in story.Scenes.OrderBy(s => s.Order))
        {
            if (scene.Background?.ImagePath is { } path)
            {
                items.Add(new ImageItem($"Сцена «{scene.Title}»: фон", scene.Id, path));
            }
        }

        return items.GroupBy(i => i.Path).Select(g => g.First()).ToList();
    }
}
