using FreebetTracker.Web.Services;
using Microsoft.Extensions.Logging;

namespace FreebetTracker.Functions;

public sealed class FreebetReminderRunner(IFreebetReminderService reminderService, ILogger<FreebetReminderRunner> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting freebet reminder processing.");

        await reminderService.SendReminderAsync(cancellationToken);

        logger.LogInformation("Freebet reminder processing completed.");
    }
}
