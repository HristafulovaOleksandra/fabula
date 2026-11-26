using Fabula.Api.Auth;
using Fabula.Api.Entities;

namespace Fabula.Api.Contracts;

public static class Mapping
{
    public static TagDto ToDto(this Tag tag) => new(tag.Id, tag.Name, tag.Kind, tag.Status);

    private static IReadOnlyList<TagDto> VisibleTags(Story story, bool includePending) => story.Tags
        .Where(t => t.Status == ModerationStatus.Approved || (includePending && t.Status == ModerationStatus.Pending))
        .OrderBy(t => t.Kind).ThenBy(t => t.Name).Select(t => t.ToDto()).ToList();

    public static SceneDto ToDto(this Scene scene) => new(
        scene.Id,
        scene.Title,
        scene.Order,
        scene.BackgroundId,
        scene.Background?.ImagePath,
        scene.Characters.OrderBy(c => c.Position).Select(c => new CharacterDto(
            c.Position, c.StoryCharacterId, c.StoryCharacter?.Name, c.StoryCharacter?.ImageFor(c.Emotion), c.Emotion,
            c.X, c.Y, c.Scale, c.Flip)).ToList(),
        scene.Lines.OrderBy(l => l.Order).Select(l => new LineDto(l.Id, l.Speaker, l.Text, l.Level, l.LevelScore, l.LevelCheckedAt != null)).ToList(),
        scene.Choices.OrderBy(c => c.Order).Select(c => new ChoiceDto(c.Id, c.Text, c.NextSceneId, c.PriceCrystals)).ToList(),
        scene.BackgroundFocusX,
        scene.BackgroundFocusY,
        scene.BackgroundZoom);

    public static StorySummaryDto ToSummary(this Story story) => new(
        story.Id,
        story.Title,
        story.Description,
        story.CoverImagePath,
        story.CoverFocusX,
        story.CoverFocusY,
        story.CoverZoom,
        story.Level,
        story.PriceCrystals,
        story.Status,
        story.Author?.Username ?? "",
        story.Author is null ? null : NamePolicy.PublicName(story.Author),
        VisibleTags(story, includePending: false),
        story.Scenes.Count,
        story.UpdatedAt);

    public static StoryDetailDto ToDetail(this Story story, bool forEditor = false) => new(
        story.Id,
        story.AuthorId,
        story.Title,
        story.Description,
        story.CoverImagePath,
        story.CoverFocusX,
        story.CoverFocusY,
        story.CoverZoom,
        story.Level,
        story.PriceCrystals,
        story.Status,
        story.StartSceneId,
        VisibleTags(story, forEditor),
        story.Words.OrderBy(w => w.Word).Select(w => new StoryWordDto(w.Word, w.Translation)).ToList(),
        story.AllowedWords.Order().ToList(),
        story.Cast.OrderBy(c => c.Order).ThenBy(c => c.Name).Select(c => c.ToDto()).ToList(),
        story.Scenes.OrderBy(s => s.Order).Select(s => s.ToDto()).ToList());

    public static StoryCharacterDto ToDto(this StoryCharacter character) =>
        new(character.Id, character.Name, character.States.OrderBy(s => s.Order)
            .Select(s => new CharacterStateDto(s.Id, s.Key, s.ImagePath)).ToList());

    public static BackgroundDto ToDto(this Background background) =>
        new(background.Id, background.Name, background.ImagePath, background.OwnerId is null);
}
