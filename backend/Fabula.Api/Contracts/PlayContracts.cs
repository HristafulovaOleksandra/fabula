using System.ComponentModel.DataAnnotations;
using Fabula.Api.Economy;

namespace Fabula.Api.Contracts;

public record PlaySessionDto(Guid SessionId, Guid StoryId, Guid? CurrentSceneId, bool Free, IReadOnlyList<Guid> PurchasedChoiceIds);

public record MakeChoiceRequest(Guid ChoiceId);

public record ChoiceResultDto(Guid? NextSceneId, bool Finished, int Spent, int Balance);

public record PlaySummaryDto(
    int ScenesVisited,
    int ChoicesMade,
    IReadOnlyList<StoryWordDto> WordsSaved,
    int CrystalsEarned,
    double Minutes,
    bool Completed,
    bool Free,
    IReadOnlyList<Reward> Rewards,
    int Balance,
    StreakDto Streak);

public record SaveWordRequest(
    [Required, MinLength(1), MaxLength(64)] string Word,
    [MaxLength(128)] string? Translation,
    Guid? StoryId);

public record UserWordDto(string Word, string Translation, int TimesSeen, DateTime FirstSeenAt, DateTime LastSeenAt);
