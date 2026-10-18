using FreebetTracker.Web.Services;
using Microsoft.Extensions.Configuration;

namespace FreebetTracker.Tests.Services;

[TestClass]
[Ignore("Manuel integrationstest. Fjern Ignore-attributten for at sende en rigtig mail.")]
public sealed class GmailEmailSenderManualTests
{
    [TestMethod]
    public async Task SendAsync_ShouldSendRealEmailThroughGmail()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("ea99bd74 - ae24 - 4b89 - b2b1 - c2c73c934fec")
            .AddUserSecrets<GmailEmailSenderManualTests>()
            .Build();

        var sender = new GmailEmailSender(configuration);

        var recipient = configuration["FreebetReminder:Recipient"]
            ?? throw new InvalidOperationException("FreebetReminder:Recipient is not configured in User Secrets.");

        // Act
        await sender.SendAsync(
            recipient,
            "FreebetTracker – testmail",
            "Dette er en testmail fra FreebetTracker via Gmail SMTP.");

        // Assert
        // Hvis SendAsync gennemfører uden exception, accepterede SMTP-serveren mailen.
        // Kontrollér også indbakken hos modtageren.
    }
}
