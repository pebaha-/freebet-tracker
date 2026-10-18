using System.Globalization;
using FreebetTracker.Web.Models;
using Microsoft.Extensions.Options;

namespace FreebetTracker.Web.Services;

public sealed class FreebetReminderService(IFreebetRepository repository, IEmailSender emailSender, IOptions<FreebetReminderOptions> options) : IFreebetReminderService
{
    public async Task SendReminderAsync(CancellationToken cancellationToken = default)
    {
        var freebets = await repository.GetAllAsync(cancellationToken);
        var selected = FreebetService.SelectForReminder(freebets);

        // Undlad at sende en tom påmindelsesmail.
        if (selected.Count == 0)
        {
            return;
        }

        var recipient = options.Value.Recipient;

        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new InvalidOperationException("The reminder email recipient is not configured.");
        }

        var subject = "Freebet-påmindelser";
        var body = BuildEmailBody(selected);

        await emailSender.SendAsync(recipient, subject, body, cancellationToken);
    }

    private static string BuildEmailBody(IReadOnlyList<Freebet> freebets)
    {
        var culture = CultureInfo.GetCultureInfo("da-DK");
        var lines = new List<string>
        {
            "Her er dine freebets, der kræver opmærksomhed:",
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
