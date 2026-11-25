using Fabula.Api.Entities;

namespace Fabula.Api.Auth;

public static class NamePolicy
{
    public static string? VisibleName(User user, Guid? viewerId, bool viewerIsAcceptedTeacher) =>
        user.Id == viewerId
        || user.NameVisibility == NameVisibility.Public
        || (user.NameVisibility == NameVisibility.Teachers && viewerIsAcceptedTeacher)
            ? user.DisplayName
            : null;

    public static string? PublicName(User user) =>
        user.NameVisibility == NameVisibility.Public ? user.DisplayName : null;
}
