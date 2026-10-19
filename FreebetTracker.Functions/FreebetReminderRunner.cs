using FreebetTracker.Web.Services;
using Microsoft.Extensions.Logging;

namespace FreebetTracker.Functions;

public sealed class FreebetReminderRunner(IFreebetReminderService reminderService, ILogger<FreebetReminderRunner> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting freebet reminder run.");

        try
        {
            await reminderService.SendReminderAsync(cancellationToken);
            logger.LogInformation("Freebet reminder run completed successfully.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Freebet reminder run was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Freebet reminder run failed.");
            throw;
        }
    }
}
