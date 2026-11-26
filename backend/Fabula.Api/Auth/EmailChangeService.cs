using System.Security.Cryptography;
using Fabula.Api.Data;
using Fabula.Api.Email;
using Fabula.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Auth;

public enum EmailChangeStart
{
    Sent,
    WrongPassword,
    SameEmail,
    Taken,
    TooOften
}

public class EmailChangeService(FabulaDbContext db, EmailSender email)
{
    public async Task<EmailChangeStart> RequestAsync(User user, string newEmail, string password, CancellationToken cancellationToken)
    {
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return EmailChangeStart.WrongPassword;
        }

        var normalized = newEmail.Trim().ToLowerInvariant();
        if (normalized == user.Email)
        {
            return EmailChangeStart.SameEmail;
        }

        if (await db.Users.AnyAsync(u => u.Email == normalized, cancellationToken))
        {
            return EmailChangeStart.Taken;
        }

        var now = DateTime.UtcNow;
        var recent = await db.EmailChangeCodes
            .Where(c => c.UserId == user.Id && c.CreatedAt > now.AddHours(-1))
            .ToListAsync(cancellationToken);
        if (recent.Count >= PasswordResetService.MaxCodesPerHour || recent.Any(c => c.CreatedAt > now - PasswordResetService.ResendCooldown))
        {
            return EmailChangeStart.TooOften;
        }

        foreach (var active in recent.Where(c => c.UsedAt is null))
        {
            active.UsedAt = now;
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        db.EmailChangeCodes.Add(new EmailChangeCode
        {
            UserId = user.Id,
            NewEmail = normalized,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
            CreatedAt = now,
            ExpiresAt = now + PasswordResetService.CodeLifetime
        });
        await db.SaveChangesAsync(cancellationToken);

        await email.SendAsync(
            normalized,
            "Fabula: підтвердження нової пошти",
            $"Привіт, {user.DisplayName}!\n\nКод для підтвердження нової пошти: {code}\n\n" +
            $"Код діє {PasswordResetService.CodeLifetime.TotalMinutes:0} хвилин. Якщо ти не змінював(-ла) пошту у Fabula, просто проігноруй цей лист.",
            cancellationToken);
        return EmailChangeStart.Sent;
    }

    public async Task<string?> ConfirmAsync(User user, string code, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var active = await db.EmailChangeCodes
            .Where(c => c.UserId == user.Id && c.UsedAt == null && c.ExpiresAt > now)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (active is null || active.Attempts >= PasswordResetService.MaxAttempts)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(code, active.CodeHash))
        {
            active.Attempts++;
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (await db.Users.AnyAsync(u => u.Email == active.NewEmail && u.Id != user.Id, cancellationToken))
        {
            active.UsedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        active.UsedAt = now;
        user.Email = active.NewEmail;
        await db.SaveChangesAsync(cancellationToken);
        return user.Email;
    }
}
