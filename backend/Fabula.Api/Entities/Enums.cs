namespace Fabula.Api.Entities;

public enum UserRole
{
    Student,
    Author,
    Admin
}

public enum CefrLevel
{
    A1,
    A2,
    B1,
    B2,
    C
}

public enum StoryStatus
{
    Draft,
    PendingModeration,
    Published,
    Blocked
}

public enum TagKind
{
    Genre,
    Topic
}

public enum CharacterPosition
{
    Left,
    Right
}

public enum TransactionReason
{
    StoryCompleted,
    DailyGoal,
    StreakBonus,
    PaidChoice,
    PaidStory,
    StreakFreeze,
    WelcomeBonus,
    AdminAdjustment
}

public enum ModerationTarget
{
    Story,
    Line,
    Image
}

public enum ModerationStatus
{
    Pending,
    Approved,
    Rejected,
    Appealed
}

public enum TeachingStatus
{
    Pending,
    Accepted
}

public enum NameVisibility
{
    Private,
    Teachers,
    Public
}
