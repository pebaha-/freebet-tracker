using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FreebetTracker.Web.Services;

public sealed class GmailEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        var email = configuration["Gmail:Address"]
            ?? throw new InvalidOperationException(
                "Gmail address is not configured.");

        var appPassword = configuration["Gmail:AppPassword"]
            ?? throw new InvalidOperationException(
                "Gmail app password is not configured.");

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(email));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient();

        await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(email, appPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
