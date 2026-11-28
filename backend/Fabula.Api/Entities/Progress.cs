namespace Fabula.Api.Entities;

public class UserWord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Lemma { get; set; }
    public required string SurfaceForm { get; set; }
    public string Translation { get; set; } = "";
    public string LearningLanguage { get; set; } = "en";
    public string UiLanguage { get; set; } = "uk";
    public int HistorySeen { get; set; }
    public int HistoryCorrect { get; set; }
    public int SessionSeen { get; set; }
    public int SessionCorrect { get; set; }
    public int TimesTapped { get; set; }
    public int CorrectInRow { get; set; }
    public bool MarkedKnown { get; set; }
    public DateTime? LearnedAt { get; set; }
    public DateTime? LastPracticedAt { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public Guid? SourceStoryId { get; set; }
}

public class GameSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid StoryId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public List<Guid> ScenePath { get; set; } = [];
    public List<Guid> ChoiceIds { get; set; } = [];
    public int CrystalsEarned { get; set; }
}

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Amount { get; set; }
    public TransactionReason Reason { get; set; }
    public Guid? StoryId { get; set; }
    public Guid? ChoiceId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Streak
{
    public Guid UserId { get; set; }
    public int CurrentDays { get; set; }
    public int LongestDays { get; set; }
    public DateOnly? LastActiveDate { get; set; }
    public int RescuableDays { get; set; }
    public DateOnly? RescuableUntil { get; set; }
}

public class TeachingLink
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public User? Teacher { get; set; }
    public Guid StudentId { get; set; }
    public User? Student { get; set; }
    public TeachingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
}

public class Follow
{
    public Guid FollowerId { get; set; }
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PasswordResetCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string CodeHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? UsedAt { get; set; }
}

public class Assignment
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid StudentId { get; set; }
    public Guid StoryId { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ModerationItem
{
    public Guid Id { get; set; }
    public ModerationTarget TargetType { get; set; }
    public Guid TargetId { get; set; }
    public required string Reason { get; set; }
    public double Score { get; set; }
    public ModerationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class EmailChangeCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string NewEmail { get; set; }
    public required string CodeHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? UsedAt { get; set; }
}
