using System.Security.Claims;
using Fabula.Api.Auth;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Moderation;

public static class AgePolicy
{
    public const int MinRegistrationAge = 5;
    public const int MaxRegistrationAge = 100;

    public static int? YearsOld(DateOnly? birthDate, DateOnly today)
    {
        if (birthDate is null)
        {
            return null;
        }

        var years = today.Year - birthDate.Value.Year;
        return birthDate.Value.AddYears(years) > today ? years - 1 : years;
    }

    public static string? BirthDateProblem(DateOnly birthDate)
    {
        var years = YearsOld(birthDate, DateOnly.FromDateTime(DateTime.UtcNow));
        return years is null || years < MinRegistrationAge || years > MaxRegistrationAge
            ? $"Перевір дату народження: вік має бути від {MinRegistrationAge} до {MaxRegistrationAge} років."
            : null;
    }

    public static AgeRating Allowed(DateOnly? birthDate, DateOnly today) => YearsOld(birthDate, today) switch
    {
        >= 16 => AgeRating.Mature,
        >= 12 => AgeRating.Teen,
        _ => AgeRating.All
    };

    public static AgeRating[] Upto(AgeRating max) => Enum.GetValues<AgeRating>().Where(r => r <= max).ToArray();

    public static string Label(AgeRating rating) => rating switch
    {
        AgeRating.Mature => "16+",
        AgeRating.Teen => "12+",
        _ => "6+"
    };

    public static async Task<AgeRating> ViewerAllowedAsync(this FabulaDbContext db, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return AgeRating.All;
        }

        if (principal.IsAdmin())
        {
            return AgeRating.Mature;
        }

        var userId = principal.UserId();
        var birthDate = await db.Users.Where(u => u.Id == userId).Select(u => u.BirthDate).SingleOrDefaultAsync(cancellationToken);
        return Allowed(birthDate, DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
