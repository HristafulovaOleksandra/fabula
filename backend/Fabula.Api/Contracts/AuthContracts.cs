using System.ComponentModel.DataAnnotations;
using Fabula.Api.Entities;

namespace Fabula.Api.Contracts;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, RegularExpression("^[a-zA-Z0-9_.]{3,30}$", ErrorMessage = "Нік: 3–30 латинських літер, цифр, «_» або «.».")] string Username,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [Required, MinLength(2), MaxLength(100)] string DisplayName,
    UserRole Role = UserRole.Student);

public record LoginRequest(
    [MaxLength(256)] string? Login,
    [MaxLength(256)] string? Email,
    [Required] string Password);

public record PasswordResetRequest([Required, EmailAddress] string Email);

public record PasswordResetConfirmRequest(
    [Required, EmailAddress] string Email,
    [Required, RegularExpression("^[0-9]{6}$", ErrorMessage = "Код складається з 6 цифр.")] string Code,
    [Required, MinLength(8), MaxLength(128)] string NewPassword);

public record DeleteAccountRequest([Required] string Password);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8), MaxLength(128)] string NewPassword);

public record EmailChangeRequest(
    [Required, EmailAddress, MaxLength(256)] string NewEmail,
    [Required] string Password);

public record EmailChangeConfirmRequest(
    [Required, RegularExpression("^[0-9]{6}$", ErrorMessage = "Код складається з 6 цифр.")] string Code);

public record UpdateProfileRequest(
    [MinLength(2), MaxLength(100)] string? DisplayName,
    NameVisibility? NameVisibility);

public record UserDto(
    Guid Id,
    string Email,
    string Username,
    string DisplayName,
    NameVisibility NameVisibility,
    UserRole Role,
    bool IsTeacher,
    CefrLevel? Level,
    IReadOnlyList<string> FavoriteTopics,
    int CrystalBalance,
    AvatarDto Avatar)
{
    public static UserDto From(User user, bool isTeacher) => new(
        user.Id, user.Email, user.Username, user.DisplayName, user.NameVisibility, user.Role,
        isTeacher, user.Level, user.FavoriteTopics, user.CrystalBalance, AvatarDto.From(user));
}

public record AvatarDto(string? Path, double FocusX, double FocusY, double Zoom)
{
    public static AvatarDto From(User user) => new(user.AvatarPath, user.AvatarFocusX, user.AvatarFocusY, user.AvatarZoom);
}

public record AvatarFramingRequest(
    [Range(0, 100)] double FocusX,
    [Range(0, 100)] double FocusY,
    [Range(1, 3)] double Zoom);

public record AuthResponse(string Token, UserDto User);

public record PublicUserDto(Guid Id, string Username, string? DisplayName, bool IsTeacher, AvatarDto? Avatar = null);

public record AuthorProfileDto(PublicUserDto Author, int FollowerCount, bool IsFollowedByMe, IReadOnlyList<StorySummaryDto> Stories, AuthorStatsDto Stats);

public record CountDto(string Name, int Count);

public record AuthorStatsDto(int StoryCount, int ReaderCount, double? Rating, int RatingCount, IReadOnlyList<CountDto> Levels, IReadOnlyList<CountDto> Genres);

public record InviteStudentRequest([Required, MinLength(3), MaxLength(256)] string EmailOrUsername);

public record TeachingLinkDto(Guid Id, PublicUserDto Teacher, PublicUserDto Student, TeachingStatus Status, DateTime CreatedAt, DateTime? AcceptedAt);
