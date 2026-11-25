using Fabula.Api.Auth;
using Fabula.Api.Entities;

namespace Fabula.Api.Contracts;

public static class Mapping
{
    public static TagDto ToDto(this Tag tag) => new(tag.Id, tag.Name, tag.Kind);

    public static SceneDto ToDto(this Scene scene) => new(
        scene.Id,
        scene.Title,
        scene.Order,
        scene.BackgroundId,
        scene.Background?.ImagePath,
        scene.Characters.OrderBy(c => c.Position).Select(c => new CharacterDto(c.Position, c.Name, c.ImagePath, c.Emotion)).ToList(),
        scene.Lines.OrderBy(l => l.Order).Select(l => new LineDto(l.Id, l.Speaker, l.Text, l.Level, l.LevelScore)).ToList(),
        scene.Choices.OrderBy(c => c.Order).Select(c => new ChoiceDto(c.Id, c.Text, c.NextSceneId, c.PriceCrystals)).ToList());

    public static StorySummaryDto ToSummary(this Story story) => new(
        story.Id,
        story.Title,
        story.Description,
        story.CoverImagePath,
        story.Level,
        story.PriceCrystals,
        story.Status,
        story.Author?.Username ?? "",
        story.Author is null ? null : NamePolicy.PublicName(story.Author),
        story.Tags.OrderBy(t => t.Kind).ThenBy(t => t.Name).Select(t => t.ToDto()).ToList(),
        story.Scenes.Count,
        story.UpdatedAt);

    public static StoryDetailDto ToDetail(this Story story) => new(
        story.Id,
        story.AuthorId,
        story.Title,
        story.Description,
        story.CoverImagePath,
        story.Level,
        story.PriceCrystals,
        story.Status,
        story.StartSceneId,
        story.Tags.OrderBy(t => t.Kind).ThenBy(t => t.Name).Select(t => t.ToDto()).ToList(),
        story.Words.OrderBy(w => w.Word).Select(w => new StoryWordDto(w.Word, w.Translation)).ToList(),
        story.Scenes.OrderBy(s => s.Order).Select(s => s.ToDto()).ToList());

    public static BackgroundDto ToDto(this Background background) =>
        new(background.Id, background.Name, background.ImagePath, background.OwnerId is null);
}
