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
[Route("api/stories/{storyId:guid}/scenes")]
public class ScenesController(FabulaDbContext db) : ControllerBase
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
        db.Characters.AddRange(request.Characters.Select(c => new Character
        {
            SceneId = scene.Id,
            Position = c.Position,
            Name = c.Name.Trim(),
            ImagePath = c.ImagePath,
            Emotion = string.IsNullOrWhiteSpace(c.Emotion) ? "neutral" : c.Emotion.Trim()
        }));
        db.Lines.AddRange(request.Lines.Select((l, index) => BuildLine(scene.Id, index, l, previousLines)));
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
        if (request.Id is not null && previous.TryGetValue(request.Id.Value, out var old) && old.Text == text)
        {
            line.Level = old.Level;
            line.LevelScore = old.LevelScore;
            line.LevelCheckedAt = old.LevelCheckedAt;
        }

        return line;
    }

    private async Task<Story?> LoadEditableStoryAsync(Guid storyId, CancellationToken cancellationToken)
    {
        var story = await StoryRepository.LoadFullAsync(db, storyId, cancellationToken, tracking: true);
        return story is not null && (story.AuthorId == User.UserId() || User.IsAdmin()) ? story : null;
    }
}
