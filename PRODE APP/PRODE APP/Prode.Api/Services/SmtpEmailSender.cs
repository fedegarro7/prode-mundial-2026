using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Prode.Api.Services;

public interface IEmailSender
{
    Task SendPasswordResetAsync(
        string toEmail,
        string token,
        DateTime expiresAt,
        CancellationToken cancellationToken = default
    );
}

public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "Prode Mundial 2026";

    public bool EnableSsl { get; set; } = true;
}

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _smtpOptions;
    private readonly PasswordResetOptions _passwordResetOptions;

    public SmtpEmailSender(
        IOptions<SmtpEmailOptions> smtpOptions,
        IOptions<PasswordResetOptions> passwordResetOptions
    )
    {
        _smtpOptions = smtpOptions.Value;
        _passwordResetOptions = passwordResetOptions.Value;
    }

    public async Task SendPasswordResetAsync(
        string toEmail,
        string token,
        DateTime expiresAt,
        CancellationToken cancellationToken = default
    )
    {
        EnsureConfigured();

        using var message = new MailMessage
        {
            From = new MailAddress(_smtpOptions.FromEmail, _smtpOptions.FromName),
            Subject = "Recuperacion de contrasena - Prode Mundial 2026",
            Body = BuildPasswordResetBody(toEmail, token, expiresAt),
            IsBodyHtml = false
        };

        message.To.Add(toEmail);

        using var client = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
        {
            EnableSsl = _smtpOptions.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_smtpOptions.Username))
        {
            client.Credentials = new NetworkCredential(
                _smtpOptions.Username,
                _smtpOptions.Password
            );
        }

        await client.SendMailAsync(message, cancellationToken);
    }

    private void EnsureConfigured()
    {
        if (
            string.IsNullOrWhiteSpace(_smtpOptions.Host) ||
            string.IsNullOrWhiteSpace(_smtpOptions.FromEmail)
        )
        {
            throw new InvalidOperationException(
                "SMTP email is not configured."
            );
        }
    }

    private string BuildPasswordResetBody(
        string toEmail,
        string token,
        DateTime expiresAt
    )
    {
        var resetUrl = BuildResetUrl(toEmail, token);

        return
            "Recibimos una solicitud para recuperar tu contrasena.\n\n" +
            $"Codigo de recuperacion: {token}\n" +
            $"Vence UTC: {expiresAt:yyyy-MM-dd HH:mm}\n\n" +
            (
                string.IsNullOrWhiteSpace(resetUrl)
                    ? string.Empty
                    : $"Tambien podes abrir este link: {resetUrl}\n\n"
            ) +
            "Si no pediste este cambio, ignora este email.";
    }

    private string? BuildResetUrl(string toEmail, string token)
    {
        if (string.IsNullOrWhiteSpace(_passwordResetOptions.ResetUrl))
        {
            return null;
        }

        var separator =
            _passwordResetOptions.ResetUrl.Contains('?') ? '&' : '?';

        return
            $"{_passwordResetOptions.ResetUrl}{separator}" +
            $"email={Uri.EscapeDataString(toEmail)}&" +
            $"token={Uri.EscapeDataString(token)}";
    }
}
