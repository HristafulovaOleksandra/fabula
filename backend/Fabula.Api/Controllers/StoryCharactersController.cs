using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Fabula.Api.Moderation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.CanAuthor)]
[Route("api/stories/{storyId:guid}/characters")]
public class StoryCharactersController(FabulaDbContext db, StoryModeration moderation) : ControllerBase
{
    private const string CharacterImagesPrefix = "/uploads/characters/";

    [HttpPost]
    public async Task<ActionResult<StoryCharacterDto>> Create(Guid storyId, StoryCharacterUpsertRequest request, CancellationToken cancellationToken)
    {
        var story = await LoadEditableStoryAsync(storyId, cancellationToken);
        if (story is null)
        {
            return NotFound();
        }

        var problem = Validate(story, request, null);
        if (problem is not null)
        {
            return problem;
        }

        var character = new StoryCharacter
        {
            StoryId = story.Id,
            Name = request.Name.Trim(),
            Order = story.Cast.Count == 0 ? 0 : story.Cast.Max(c => c.Order) + 1
        };
        ApplyStates(character, request, []);
        db.StoryCharacters.Add(character);
        story.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(character.ToDto());
    }

    [HttpPut("{characterId:guid}")]
    public async Task<ActionResult<StoryCharacterDto>> Update(
        Guid storyId, Guid characterId, StoryCharacterUpsertRequest request, CancellationToken cancellationToken)
    {
        var story = await LoadEditableStoryAsync(storyId, cancellationToken);
        var character = story?.Cast.SingleOrDefault(c => c.Id == characterId);
        if (story is null || character is null)
        {
            return NotFound();
        }

        var problem = Validate(story, request, characterId);
        if (problem is not null)
        {
            return problem;
        }

        var oldName = character.Name;
        var newName = request.Name.Trim();
        if (oldName != newName)
        {
            var sceneIds = story.Scenes.Select(s => s.Id).ToList();
            await db.Lines.Where(l => sceneIds.Contains(l.SceneId) && l.Speaker == oldName)
                .ExecuteUpdateAsync(update => update.SetProperty(l => l.Speaker, newName), cancellationToken);
        }

        character.Name = newName;
        ApplyStates(character, request, PlacementsOf(story, character.Id));
        story.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        if (story.Status is StoryStatus.Published or StoryStatus.PendingModeration)
        {
            await moderation.ApplyAsync(storyId, publishing: false, cancellationToken);
        }

        return Ok(character.ToDto());
    }

    [HttpDelete("{characterId:guid}")]
    public async Task<IActionResult> Delete(Guid storyId, Guid characterId, CancellationToken cancellationToken)
    {
        var story = await LoadEditableStoryAsync(storyId, cancellationToken);
        var character = story?.Cast.SingleOrDefault(c => c.Id == characterId);
        if (story is null || character is null)
        {
            return NotFound();
        }

        db.StoryCharacters.Remove(character);
        story.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private ActionResult? Validate(Story story, StoryCharacterUpsertRequest request, Guid? currentId)
    {
        var name = request.Name.Trim();
        if (story.Cast.Any(c => c.Id != currentId && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationProblem("Персонаж з таким іменем уже є в цій історії.");
        }

        var states = request.States ?? [];
        if (states.Any(s => s.ImagePath is not null && !s.ImagePath.StartsWith(CharacterImagesPrefix)))
        {
            return ValidationProblem("Картинку персонажа треба спершу завантажити.");
        }

        var keys = states.Select(s => NormalizeKey(s.Key)).ToList();
        if (keys.Any(k => k.Length == 0))
        {
            return ValidationProblem("У кожного стану має бути назва.");
        }

        return keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != keys.Count
            ? ValidationProblem("Назви станів персонажа не можуть повторюватися.")
            : null;
    }

    private static string NormalizeKey(string key) => string.Join(' ', key.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private void ApplyStates(StoryCharacter character, StoryCharacterUpsertRequest request, IReadOnlyList<Character> placements)
    {
        var incoming = (request.States ?? []).Select((s, index) => (s.Id, Key: NormalizeKey(s.Key), s.ImagePath, Order: index)).ToList();
        var kept = incoming.Where(s => s.Id is not null).Select(s => s.Id!.Value).ToHashSet();

        foreach (var removed in character.States.Where(s => !kept.Contains(s.Id)).ToList())
        {
            var stillAvailable = Emotions.All.Contains(removed.Key) || incoming.Any(s => s.Key == removed.Key);
            foreach (var placement in placements.Where(p => p.Emotion == removed.Key && !stillAvailable))
            {
                placement.Emotion = Emotions.Neutral;
            }

            character.States.Remove(removed);
            db.StoryCharacterStates.Remove(removed);
        }

        foreach (var state in incoming)
        {
            var existing = state.Id is null ? null : character.States.SingleOrDefault(s => s.Id == state.Id);
            if (existing is null)
            {
                character.States.Add(new StoryCharacterState { Key = state.Key, ImagePath = state.ImagePath, Order = state.Order });
                continue;
            }

            foreach (var placement in placements.Where(p => p.Emotion == existing.Key && existing.Key != state.Key))
            {
                placement.Emotion = state.Key;
            }

            existing.Key = state.Key;
            existing.ImagePath = state.ImagePath;
            existing.Order = state.Order;
        }
    }

    private static IReadOnlyList<Character> PlacementsOf(Story story, Guid characterId) =>
        story.Scenes.SelectMany(s => s.Characters).Where(c => c.StoryCharacterId == characterId).ToList();

    private async Task<Story?> LoadEditableStoryAsync(Guid storyId, CancellationToken cancellationToken)
    {
        var story = await StoryRepository.LoadFullAsync(db, storyId, cancellationToken, tracking: true);
        return story is not null && (story.AuthorId == User.UserId() || User.IsAdmin()) ? story : null;
    }
}
