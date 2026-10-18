using Azure.Data.Tables;
using FreebetTracker.Web.Data;
using FreebetTracker.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace FreebetTracker.Tests.Services;

[TestClass]
public sealed class FreebetReminderServiceIntegrationTests
{
    [TestMethod]
    [Ignore("Manuel integrationstest. Fjern Ignore-attributten for at sende en rigtig mail.")]
    public async Task SendReminderAsync_Should_RunWithRealAzureStorageAndGmail()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<FreebetReminderService>()
            .AddUserSecrets<FreebetReminderServiceIntegrationTests>()
            .Build();

        var connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Storage connection string is not configured.");

        var recipient = configuration["FreebetReminder:Recipient"]
            ?? throw new InvalidOperationException("Freebet reminder recipient is not configured.");

        var tableClient = new TableClient(connectionString, "Freebets");
        var repository = new FreebetTableRepository(tableClient);
        var emailSender = new GmailEmailSender(configuration);
        var options = Options.Create(new FreebetReminderOptions
        {
            Recipient = recipient
        });
        var service = new FreebetReminderService(repository, emailSender, options);

        await service.SendReminderAsync();
    }
}
