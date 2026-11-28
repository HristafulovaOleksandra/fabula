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
    StreakDto Streak,
    bool CanRate,
    int? MyRating);

public record SaveWordRequest(
    [Required, MinLength(1), MaxLength(64)] string Word,
    [MaxLength(128)] string? Translation,
    Guid? StoryId);

public record UserWordDto(
    Guid Id,
    string Word,
    string Translation,
    string? StoryTitle,
    int TimesTapped,
    int Attempts,
    int Correct,
    int CorrectInRow,
    bool Learned,
    bool MarkedKnown,
    DateTime FirstSeenAt,
    DateTime LastSeenAt);

public enum QuizDirection
{
    EnglishToUkrainian,
    UkrainianToEnglish
}

public record QuizQuestionDto(Guid WordId, QuizDirection Direction, string Prompt, IReadOnlyList<string> Options);

public record QuizAnswerRequest(QuizDirection Direction, [Required, MaxLength(200)] string Answer);

public record QuizAnswerDto(bool Correct, string CorrectAnswer, int CorrectInRow, bool Learned);

public record WordKnownRequest(bool Known);
