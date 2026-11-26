using System.ComponentModel.DataAnnotations;
using Fabula.Api.Entities;

namespace Fabula.Api.Contracts;

public record TagDto(int Id, string Name, TagKind Kind, ModerationStatus Status);

public record CreateTagRequest([Required, MinLength(2), MaxLength(30)] string Name, TagKind Kind);

public record TranslationDto(string Word, IReadOnlyList<string> Translations);

public record AllowedWordRequest([Required, MinLength(1), MaxLength(64)] string Word);

public record StoryWordDto(
    [Required, MaxLength(64)] string Word,
    [Required, MaxLength(128)] string Translation);

public record StorySummaryDto(
    Guid Id,
    string Title,
    string Description,
    string? CoverImagePath,
    double CoverFocusX,
    double CoverFocusY,
    double CoverZoom,
    CefrLevel Level,
    int PriceCrystals,
    StoryStatus Status,
    string AuthorUsername,
    string? AuthorDisplayName,
    IReadOnlyList<TagDto> Tags,
    int SceneCount,
    DateTime UpdatedAt);

public record StoryDetailDto(
    Guid Id,
    Guid AuthorId,
    string Title,
    string Description,
    string? CoverImagePath,
    double CoverFocusX,
    double CoverFocusY,
    double CoverZoom,
    CefrLevel Level,
    int PriceCrystals,
    StoryStatus Status,
    Guid? StartSceneId,
    IReadOnlyList<TagDto> Tags,
    IReadOnlyList<StoryWordDto> Words,
    IReadOnlyList<string> AllowedWords,
    IReadOnlyList<StoryCharacterDto> Characters,
    IReadOnlyList<SceneDto> Scenes);

public record StoryUpsertRequest(
    [Required, MinLength(1), MaxLength(200)] string Title,
    [MaxLength(2000)] string? Description,
    string? CoverImagePath,
    CefrLevel Level,
    [Range(0, 1000)] int PriceCrystals,
    IReadOnlyList<int>? TagIds,
    IReadOnlyList<StoryWordDto>? Words,
    Guid? StartSceneId,
    [Range(0, 100)] double CoverFocusX = 50,
    [Range(0, 100)] double CoverFocusY = 50,
    [Range(1, 3)] double CoverZoom = 1);

public record CharacterDto(
    CharacterPosition Position,
    Guid StoryCharacterId,
    string? Name = null,
    string? ImagePath = null,
    [MaxLength(30)] string? Emotion = null,
    [Range(0, 100)] double? X = null,
    [Range(0, 60)] double? Y = null,
    [Range(0.4, 2)] double? Scale = null,
    bool Flip = false);

public record CharacterStateDto(Guid? Id, [Required, MinLength(1), MaxLength(30)] string Key, string? ImagePath);

public record StoryCharacterDto(Guid Id, string Name, IReadOnlyList<CharacterStateDto> States);

public record StoryCharacterUpsertRequest(
    [Required, MinLength(1), MaxLength(60)] string Name,
    [MaxLength(16)] IReadOnlyList<CharacterStateDto>? States);

public record LineDto(
    Guid? Id,
    [MaxLength(60)] string? Speaker,
    [Required, MinLength(1), MaxLength(500)] string Text,
    CefrLevel? Level = null,
    double? LevelScore = null,
    bool LevelChecked = false);

public record ChoiceDto(
    Guid? Id,
    [Required, MinLength(1), MaxLength(200)] string Text,
    Guid? NextSceneId,
    [Range(0, 1000)] int PriceCrystals);

public record SceneDto(
    Guid Id,
    string Title,
    int Order,
    Guid? BackgroundId,
    string? BackgroundImagePath,
    IReadOnlyList<CharacterDto> Characters,
    IReadOnlyList<LineDto> Lines,
    IReadOnlyList<ChoiceDto> Choices);

public record CreateSceneRequest([Required, MinLength(1), MaxLength(120)] string Title);

public record SceneUpsertRequest(
    [Required, MinLength(1), MaxLength(120)] string Title,
    Guid? BackgroundId,
    [MaxLength(2)] IReadOnlyList<CharacterDto> Characters,
    [MaxLength(100)] IReadOnlyList<LineDto> Lines,
    [MaxLength(10)] IReadOnlyList<ChoiceDto> Choices);

public record BackgroundDto(Guid Id, string Name, string ImagePath, bool IsLibrary);

public record UploadResponse(string Path);
