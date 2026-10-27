using Azure.Data.Tables;
using FreebetTracker.Web.Data;
using FreebetTracker.Web.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        // create logger factory earlier so it can be reused
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        });

        // create repository logger and repository
        var repoLogger = loggerFactory.CreateLogger<FreebetTableRepository>();
        var tableClient = new TableClient(connectionString, "Freebets");
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FreebetTableRepository(tableClient, repoLogger, memoryCache);
        var pauseStore = new TableReminderPauseStore(tableClient);

        // email sender
        var emailSender = new GmailEmailSender(configuration);

        // options unchanged
        var options = Options.Create(new FreebetReminderOptions
        {
            Recipient = recipient
        });

        // create service logger and service
        var logger = loggerFactory.CreateLogger<FreebetReminderService>();
        var service = new FreebetReminderService(repository, emailSender, options, logger, pauseStore);

        await service.SendReminderAsync();
    }
}
