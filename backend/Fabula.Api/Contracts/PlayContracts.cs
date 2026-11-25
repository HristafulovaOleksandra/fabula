using System.ComponentModel.DataAnnotations;

namespace Fabula.Api.Contracts;

public record PlaySessionDto(Guid SessionId, Guid StoryId, Guid? CurrentSceneId);

public record MakeChoiceRequest(Guid ChoiceId);

public record ChoiceResultDto(Guid? NextSceneId, bool Finished);

public record PlaySummaryDto(int ScenesVisited, int ChoicesMade, IReadOnlyList<StoryWordDto> WordsSaved, int CrystalsEarned, double Minutes);

public record SaveWordRequest(
    [Required, MinLength(1), MaxLength(64)] string Word,
    [MaxLength(128)] string? Translation,
    Guid? StoryId);

public record UserWordDto(string Word, string Translation, int TimesSeen, DateTime FirstSeenAt, DateTime LastSeenAt);
