using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Fabula.Api.Email;

public record SmtpSettings(string Host, int Port, SecureSocketOptions Security, string? Username, string? Password, string From)
{
    public static SmtpSettings FromEnvironment() => new(
        Environment.GetEnvironmentVariable("SMTP_HOST") is { Length: > 0 } host ? host : "localhost",
        int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 1025,
        Enum.TryParse<SecureSocketOptions>(Environment.GetEnvironmentVariable("SMTP_SECURITY"), true, out var security)
            ? security
            : SecureSocketOptions.None,
        Environment.GetEnvironmentVariable("SMTP_USER") is { Length: > 0 } user ? user : null,
        Environment.GetEnvironmentVariable("SMTP_PASSWORD") is { Length: > 0 } password ? password : null,
        Environment.GetEnvironmentVariable("SMTP_FROM") is { Length: > 0 } from ? from : "Fabula <no-reply@fabula.local>");
}

public class EmailSender(SmtpSettings settings, ILogger<EmailSender> logger)
{
    public async Task<bool> SendAsync(string to, string subject, string text, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = text };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(settings.Host, settings.Port, settings.Security, cancellationToken);
            if (settings.Username is not null)
            {
                await client.AuthenticateAsync(settings.Username, settings.Password ?? "", cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Не вдалося надіслати лист на {Recipient}", to);
            return false;
        }
    }
}
