using System.Globalization;
using FreebetTracker.Web.Models;
using Microsoft.Extensions.Options;

namespace FreebetTracker.Web.Services;

public sealed class FreebetReminderService(IFreebetRepository repository, IEmailSender emailSender, IOptions<FreebetReminderOptions> options, ILogger<FreebetReminderService> logger) : IFreebetReminderService
{
    public async Task SendReminderAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching freebets from the repository.");

        var freebets = await repository.GetAllAsync(cancellationToken);

        logger.LogInformation("Fetched {FreebetCount} freebets from the repository.", freebets.Count);

        var selected = FreebetService.SelectForReminder(freebets);

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

        var lines = new List<string>
        {
            "Her er de af dine freebets, der udløber som de næste:",
            string.Empty
        };

        foreach (var freebet in freebets)
        {
            lines.Add($"Konto: {freebet.Account}");
            lines.Add($"Spiller: {freebet.Player}");
            lines.Add($"Beløb: {freebet.Amount.ToString("N2", culture)} kr.");
            lines.Add($"Udløber: {freebet.ExpiryDate.ToString("dd-MM-yyyy HH:mm", culture)}");

            if (!string.IsNullOrWhiteSpace(freebet.Terms))
            {
                lines.Add($"Vilkår: {freebet.Terms}");
            }

            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
