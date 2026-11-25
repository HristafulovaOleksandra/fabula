using System.Security.Cryptography;
using Fabula.Api.Data;
using Fabula.Api.Email;
using Fabula.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Auth;

public class PasswordResetService(FabulaDbContext db, EmailSender email)
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);
    public const int MaxAttempts = 5;
    public const int MaxCodesPerHour = 5;

    public async Task RequestAsync(string emailAddress, CancellationToken cancellationToken)
    {
        var normalized = emailAddress.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == normalized, cancellationToken);
        if (user is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var recent = await db.PasswordResetCodes
            .Where(c => c.UserId == user.Id && c.CreatedAt > now.AddHours(-1))
            .ToListAsync(cancellationToken);
        if (recent.Count >= MaxCodesPerHour || recent.Any(c => c.CreatedAt > now - ResendCooldown))
        {
            return;
        }

        foreach (var active in recent.Where(c => c.UsedAt is null))
        {
            active.UsedAt = now;
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        db.PasswordResetCodes.Add(new PasswordResetCode
        {
            UserId = user.Id,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
            CreatedAt = now,
            ExpiresAt = now + CodeLifetime
        });
        await db.SaveChangesAsync(cancellationToken);

        await email.SendAsync(
            user.Email,
            "Fabula: код для відновлення пароля",
            $"Привіт, {user.DisplayName}!\n\nТвій код для відновлення пароля: {code}\n\n" +
            $"Код діє {CodeLifetime.TotalMinutes:0} хвилин. Якщо ти не просив(-ла) змінити пароль, просто проігноруй цей лист.",
            cancellationToken);
    }

    public async Task<bool> ConfirmAsync(string emailAddress, string code, string newPassword, CancellationToken cancellationToken)
    {
        var normalized = emailAddress.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == normalized, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var active = await db.PasswordResetCodes
            .Where(c => c.UserId == user.Id && c.UsedAt == null && c.ExpiresAt > now)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (active is null || active.Attempts >= MaxAttempts)
        {
            return false;
        }

        if (!BCrypt.Net.BCrypt.Verify(code, active.CodeHash))
        {
            active.Attempts++;
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        active.UsedAt = now;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
