using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Fabula.Api.Moderation;
using Fabula.Api.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fabula.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController(FabulaDbContext db, ImageStorage storage, EmailChangeService emailChange, StoryModeration moderation) : ControllerBase
{
    [HttpPost("avatar")]
    [RequestSizeLimit(ImageStorage.MaxBytes + 64 * 1024)]
    public async Task<ActionResult<UserDto>> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([User.UserId()], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        string path;
        try
        {
            path = await storage.SaveAsync(file, "avatars", cancellationToken);
        }
        catch (ImageRejectedException exception)
        {
            return ValidationProblem(exception.Message);
        }

        var check = await moderation.CheckImageAsync(path, cancellationToken);
        if (check is not null && (check.Verdict != ModerationVerdict.Ok || check.Age != AgeRating.All))
        {
            storage.DeleteIfOwned(path);
            return ValidationProblem("Це фото не підходить для профілю: воно не пройшло автоматичну перевірку.");
        }

        var previous = user.AvatarPath;
        user.AvatarPath = path;
        user.AvatarFocusX = 50;
        user.AvatarFocusY = 50;
        user.AvatarZoom = 1;
        await db.SaveChangesAsync(cancellationToken);
        storage.DeleteIfOwned(previous);
        return Ok(UserDto.From(user, await db.IsTeacherAsync(user.Id, cancellationToken)));
    }

    [HttpPut("avatar/framing")]
    public async Task<ActionResult<UserDto>> FrameAvatar(AvatarFramingRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([User.UserId()], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (user.AvatarPath is null)
        {
            return ValidationProblem("Спершу додай фото профілю.");
        }

        user.AvatarFocusX = request.FocusX;
        user.AvatarFocusY = request.FocusY;
        user.AvatarZoom = request.Zoom;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(UserDto.From(user, await db.IsTeacherAsync(user.Id, cancellationToken)));
    }

    [HttpDelete("avatar")]
    public async Task<ActionResult<UserDto>> RemoveAvatar(CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([User.UserId()], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var previous = user.AvatarPath;
        user.AvatarPath = null;
        await db.SaveChangesAsync(cancellationToken);
        storage.DeleteIfOwned(previous);
        return Ok(UserDto.From(user, await db.IsTeacherAsync(user.Id, cancellationToken)));
    }

    [EnableRateLimiting(RateLimits.Auth)]
    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([User.UserId()], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return ValidationProblem("Поточний пароль неправильний.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            return ValidationProblem("Новий пароль має відрізнятися від поточного.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [EnableRateLimiting(RateLimits.Auth)]
    [HttpPost("email")]
    public async Task<IActionResult> RequestEmailChange(EmailChangeRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([User.UserId()], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return await emailChange.RequestAsync(user, request.NewEmail, request.Password, cancellationToken) switch
        {
            EmailChangeStart.Sent => Accepted(new { message = "Ми надіслали код на нову пошту." }),
            EmailChangeStart.WrongPassword => ValidationProblem("Пароль неправильний."),
            EmailChangeStart.SameEmail => ValidationProblem("Це вже твоя поточна пошта."),
            EmailChangeStart.Taken => ValidationProblem("Ця пошта вже використовується іншим акаунтом."),
            _ => ValidationProblem("Новий код можна попросити не частіше ніж раз на хвилину і не більше 5 разів на годину.")
        };
    }

    [EnableRateLimiting(RateLimits.Auth)]
    [HttpPost("email/confirm")]
    public async Task<ActionResult<UserDto>> ConfirmEmailChange(EmailChangeConfirmRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([User.UserId()], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return await emailChange.ConfirmAsync(user, request.Code, cancellationToken) is null
            ? ValidationProblem("Код неправильний, прострочений або вичерпано спроби. Попроси новий код.")
            : Ok(UserDto.From(user, await db.IsTeacherAsync(user.Id, cancellationToken)));
    }
}
