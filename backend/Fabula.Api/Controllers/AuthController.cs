using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(RateLimits.Auth)]
public class AuthController(FabulaDbContext db, TokenService tokens, PasswordResetService passwordReset) : ControllerBase
{
    private static readonly UserRole[] SelfRegisterRoles = [UserRole.Student, UserRole.Author];

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!SelfRegisterRoles.Contains(request.Role))
        {
            return ValidationProblem("Під час реєстрації можна обрати лише роль учня або автора.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Користувач з такою поштою вже існує." });
        }

        if (await db.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Цей нік уже зайнятий." });
        }

        var user = new User
        {
            Email = email,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName.Trim(),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow,
            Streak = new Streak()
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new AuthResponse(tokens.Create(user), UserDto.From(user, isTeacher: false)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var identifier = (request.Login ?? request.Email ?? "").Trim().TrimStart('@').ToLowerInvariant();
        if (identifier.Length == 0)
        {
            return ValidationProblem("Вкажи пошту або нік.");
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == identifier || u.Username == identifier, cancellationToken);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new ProblemDetails { Title = "Неправильна пошта, нік або пароль." });
        }

        return Ok(new AuthResponse(tokens.Create(user), UserDto.From(user, await db.IsTeacherAsync(user.Id, cancellationToken))));
    }

    [HttpPost("password-reset/request")]
    public async Task<IActionResult> RequestPasswordReset(PasswordResetRequest request, CancellationToken cancellationToken)
    {
        await passwordReset.RequestAsync(request.Email, cancellationToken);
        return Accepted(new { message = "Якщо акаунт з такою поштою існує, ми надіслали на неї код." });
    }

    [HttpPost("password-reset/confirm")]
    public async Task<IActionResult> ConfirmPasswordReset(PasswordResetConfirmRequest request, CancellationToken cancellationToken)
    {
        return await passwordReset.ConfirmAsync(request.Email, request.Code, request.NewPassword, cancellationToken)
            ? NoContent()
            : ValidationProblem("Код неправильний, прострочений або вичерпано спроби. Попроси новий код.");
    }

    [Authorize]
    [DisableRateLimiting]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([User.UserId()], cancellationToken);
        return user is null
            ? Unauthorized()
            : Ok(UserDto.From(user, await db.IsTeacherAsync(user.Id, cancellationToken)));
    }
}
