using FreebetTracker.Web.Models;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;

namespace FreebetTracker.Web.Services;

public sealed class FreebetReminderService(
    IFreebetRepository repository,
    IEmailSender emailSender,
    IOptions<FreebetReminderOptions> options,
    ILogger<FreebetReminderService> logger,
    IReminderPauseStore pauseStore) : IFreebetReminderService
{
    public async Task SendReminderAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching freebets from the repository.");

        var freebets = await repository.GetAllAsync(cancellationToken);

        logger.LogInformation("Fetched {FreebetCount} freebets from the repository.", freebets.Count);

        var selected = FreebetService.SelectForReminder(freebets);

        var pausedUntil = await pauseStore.GetPausedUntilAsync(cancellationToken);

        if (pausedUntil is not null && pausedUntil > DateTimeOffset.UtcNow)
        {
            selected = selected
                .Where(f => f.ExpiryDate <= pausedUntil.Value.LocalDateTime)
                .ToList();

            logger.LogInformation("Reminder pause active until {PausedUntil}. " + "{SelectedCount} freebets remain eligible for reminders.", pausedUntil, selected.Count);
        }

        logger.LogInformation("Selected {SelectedCount} freebets for reminders.", selected.Count);

        // Undlad at sende en tom påmindelsesmail.
        if (selected.Count == 0)
        {
            logger.LogInformation("No freebets require a reminder. No email will be sent.");
            return;
        }

        var recipient = options.Value.Recipient;

        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new InvalidOperationException("The reminder email recipient is not configured.");
        }

        var subject = "Freebet-påmindelser";
        var body = BuildEmailBody(selected);

        logger.LogInformation("Sending reminder email containing {SelectedCount} freebets.", selected.Count);

        await emailSender.SendAsync(recipient, subject, body, cancellationToken);

        logger.LogInformation("Reminder email sent successfully.");
    }


    private static string BuildEmailBody(IReadOnlyList<Freebet> freebets)
    {
        var culture = CultureInfo.GetCultureInfo("da-DK");
        var html = new System.Text.StringBuilder();

        html.Append("""
        <!DOCTYPE html>
        <html lang="da">
        <head>
            <meta charset="utf-8">
        </head>
        <body style="font-family: Arial, sans-serif; color: #333; line-height: 1.5;">
            <p>Her er de af dine freebets, der udløber som de næste:</p>
            <table style="border-collapse: collapse; width: 100%; max-width: 900px;">
                <thead>
                    <tr style="background-color: #f2f2f2;">
                        <th style="border: 1px solid #ddd; padding: 10px; text-align: left;">Konto</th>
                        <th style="border: 1px solid #ddd; padding: 10px; text-align: left;">Spiller</th>
                        <th style="border: 1px solid #ddd; padding: 10px; text-align: right;">Beløb</th>
                        <th style="border: 1px solid #ddd; padding: 10px; text-align: left;">Udløber</th>
                        <th style="border: 1px solid #ddd; padding: 10px; text-align: left;">Vilkår</th>
                    </tr>
                </thead>
                <tbody>
        """);

        foreach (var freebet in freebets)
        {
            var expiryFormat = freebet.ExpiryTimeSpecified
                ? "dd-MM-yyyy HH:mm"
                : "dd-MM-yyyy";

            html.Append("<tr>");
            html.Append($"<td style=\"border: 1px solid #ddd; padding: 10px;\">{WebUtility.HtmlEncode(freebet.Account)}</td>");
            html.Append($"<td style=\"border: 1px solid #ddd; padding: 10px;\">{WebUtility.HtmlEncode(freebet.Player)}</td>");
            html.Append($"<td style=\"border: 1px solid #ddd; padding: 10px; text-align: right; white-space: nowrap;\">{freebet.Amount.ToString("N2", culture)} kr.</td>");
            html.Append($"<td style=\"border: 1px solid #ddd; padding: 10px; white-space: nowrap;\">{freebet.ExpiryDate.ToString(expiryFormat, culture)}</td>");
            html.Append($"<td style=\"border: 1px solid #ddd; padding: 10px;\">{WebUtility.HtmlEncode(freebet.Terms ?? string.Empty)}</td>");
            html.Append("</tr>");
        }

        html.Append("""
                </tbody>
            </table>
        </body>
        </html>
        """);

        return html.ToString();
    }
}
