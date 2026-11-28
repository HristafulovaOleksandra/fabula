using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Economy;
using Fabula.Api.Entities;
using Fabula.Api.Ml;
using Fabula.Api.Moderation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.CanAuthor)]
[Route("api/stories/{storyId:guid}/scenes")]
public class ScenesController(FabulaDbContext db, EconomySettings economy, MlClient ml, StoryModeration moderation) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SceneDto>> Create(Guid storyId, CreateSceneRequest request, CancellationToken cancellationToken)
    {
        var story = await LoadEditableStoryAsync(storyId, cancellationToken);
        if (story is null)
        {
            return NotFound();
        }

        var scene = new Scene
        {
            StoryId = story.Id,
            Title = request.Title.Trim(),
            Order = story.Scenes.Count == 0 ? 0 : story.Scenes.Max(s => s.Order) + 1
        };
        db.Scenes.Add(scene);
        await db.SaveChangesAsync(cancellationToken);
        story.StartSceneId ??= scene.Id;
        story.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { storyId, sceneId = scene.Id }, scene.ToDto());
    }

    [HttpGet("{sceneId:guid}")]
    public async Task<ActionResult<SceneDto>> Get(Guid storyId, Guid sceneId, CancellationToken cancellationToken)
    {
        var story = await LoadEditableStoryAsync(storyId, cancellationToken);
        var scene = story?.Scenes.SingleOrDefault(s => s.Id == sceneId);
        return scene is null ? NotFound() : Ok(scene.ToDto());
    }

    [HttpPut("{sceneId:guid}")]
    public async Task<ActionResult<SceneDto>> Update(Guid storyId, Guid sceneId, SceneUpsertRequest request, CancellationToken cancellationToken)
    {
        var story = await LoadEditableStoryAsync(storyId, cancellationToken);
        var scene = story?.Scenes.SingleOrDefault(s => s.Id == sceneId);
        if (story is null || scene is null)
        {
            return NotFound();
        }

        if (request.Characters.Select(c => c.Position).Distinct().Count() != request.Characters.Count)
        {
            return ValidationProblem("На кожній позиції може стояти лише один персонаж.");
        }

        var castIds = story.Cast.Select(c => c.Id).ToHashSet();
        if (request.Characters.Any(c => !castIds.Contains(c.StoryCharacterId)))
        {
            return ValidationProblem("Персонажа немає в списку персонажів цієї історії.");
        }

        var castStates = story.Cast.ToDictionary(c => c.Id, c => c.States.Select(s => s.Key).Concat(Emotions.All).ToHashSet());
        if (request.Characters.Any(c => c.Emotion is not null && !castStates[c.StoryCharacterId].Contains(c.Emotion)))
        {
            return ValidationProblem("Невідомий стан персонажа.");
        }

        if (request.Choices.Any(c => !economy.IsValidChoicePrice(c.PriceCrystals)))
        {
            return ValidationProblem(economy.ChoicePriceRule);
        }

        var storySceneIds = story.Scenes.Select(s => s.Id).ToHashSet();
        if (request.Choices.Any(c => c.NextSceneId is not null && !storySceneIds.Contains(c.NextSceneId.Value)))
        {
            return ValidationProblem("Вибір може вести лише до сцени цієї історії.");
        }

        if (request.BackgroundId is not null)
        {
            var userId = User.UserId();
            var backgroundAllowed = await db.Backgrounds.AnyAsync(
                b => b.Id == request.BackgroundId && (b.OwnerId == null || b.OwnerId == userId), cancellationToken);
            if (!backgroundAllowed)
            {
                return ValidationProblem("Фон не знайдено.");
            }
        }

        var previousLines = scene.Lines.ToDictionary(l => l.Id);
        db.Characters.RemoveRange(scene.Characters);
        db.Lines.RemoveRange(scene.Lines);
        db.Choices.RemoveRange(scene.Choices);

        scene.Title = request.Title.Trim();
        scene.BackgroundId = request.BackgroundId;
        scene.BackgroundFocusX = request.BackgroundFocusX;
        scene.BackgroundFocusY = request.BackgroundFocusY;
        scene.BackgroundZoom = request.BackgroundZoom;
        db.Characters.AddRange(request.Characters.Select(c => new Character
        {
            SceneId = scene.Id,
            Position = c.Position,
            StoryCharacterId = c.StoryCharacterId,
            Emotion = c.Emotion ?? Emotions.Neutral,
            X = c.X ?? (c.Position == CharacterPosition.Left ? Stage.LeftX : Stage.RightX),
            Y = c.Y ?? 0,
            Scale = c.Scale ?? 1,
            Flip = c.Flip
        }));
        var lines = request.Lines.Select((l, index) => BuildLine(scene.Id, index, l, previousLines)).ToList();
        await RateLinesAsync(lines, story.Level, cancellationToken);
        db.Lines.AddRange(lines);
        db.Choices.AddRange(request.Choices.Select((c, index) => new Choice
        {
            SceneId = scene.Id,
            Order = index,
            Text = c.Text.Trim(),
            NextSceneId = c.NextSceneId,
            PriceCrystals = c.PriceCrystals
        }));
        story.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        if (story.Status is StoryStatus.Published or StoryStatus.PendingModeration)
        {
            await moderation.ApplyAsync(storyId, publishing: false, cancellationToken);
        }

        var saved = await LoadEditableStoryAsync(storyId, cancellationToken);
        return Ok(saved!.Scenes.Single(s => s.Id == sceneId).ToDto());
    }

    [HttpDelete("{sceneId:guid}")]
    public async Task<IActionResult> Delete(Guid storyId, Guid sceneId, CancellationToken cancellationToken)
    {
        var story = await LoadEditableStoryAsync(storyId, cancellationToken);
        var scene = story?.Scenes.SingleOrDefault(s => s.Id == sceneId);
        if (story is null || scene is null)
        {
            return NotFound();
        }

        foreach (var choice in story.Scenes.SelectMany(s => s.Choices).Where(c => c.NextSceneId == sceneId))
        {
            choice.NextSceneId = null;
        }

        if (story.StartSceneId == sceneId)
        {
            story.StartSceneId = story.Scenes.Where(s => s.Id != sceneId).OrderBy(s => s.Order).Select(s => (Guid?)s.Id).FirstOrDefault();
        }

        db.Scenes.Remove(scene);
        story.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static Line BuildLine(Guid sceneId, int index, LineDto request, IReadOnlyDictionary<Guid, Line> previous)
    {
        var text = request.Text.Trim();
        var line = new Line { SceneId = sceneId, Order = index, Speaker = request.Speaker?.Trim() ?? "", Text = text };
        if (request.Id is not null && previous.TryGetValue(request.Id.Value, out var old) && old.Text == text
            && (old.Speaker.Length == 0) == (line.Speaker.Length == 0))
        {
            line.Level = old.Level;
            line.LevelScore = old.LevelScore;
            line.LevelCheckedAt = old.LevelCheckedAt;
        }

        return line;
    }

    [HttpPost("~/api/stories/{storyId:guid}/rate-lines")]
    public async Task<ActionResult<object>> RateUnchecked(Guid storyId, CancellationToken cancellationToken)
    {
        var story = await LoadEditableStoryAsync(storyId, cancellationToken);
        if (story is null)
        {
            return NotFound();
        }

        var pending = story.Scenes.SelectMany(s => s.Lines).Where(l => l.LevelCheckedAt is null).ToList();
        await RateLinesAsync(pending, story.Level, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { rated = pending.Count(l => l.LevelCheckedAt is not null), pending = pending.Count(l => l.LevelCheckedAt is null) });
    }

    private async Task RateLinesAsync(List<Line> lines, CefrLevel storyLevel, CancellationToken cancellationToken)
    {
        var pending = lines.Where(l => l.LevelCheckedAt is null).ToList();
        var analysis = await ml.AnalyzeAsync(
            pending.Select(l => l.Text).ToList(), pending.Select(l => l.Speaker.Length == 0).ToList(), storyLevel, cancellationToken);
        if (analysis is null || analysis.Results.Count != pending.Count)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var (line, result) in pending.Zip(analysis.Results))
        {
            line.Level = Enum.TryParse<CefrLevel>(result.Level, out var level) ? level : null;
            line.LevelScore = result.Score;
            line.LevelCheckedAt = now;
        }
    }

    private async Task<Story?> LoadEditableStoryAsync(Guid storyId, CancellationToken cancellationToken)
    {
        var story = await StoryRepository.LoadFullAsync(db, storyId, cancellationToken, tracking: true);
        return story is not null && (story.AuthorId == User.UserId() || User.IsAdmin()) ? story : null;
    }
}
