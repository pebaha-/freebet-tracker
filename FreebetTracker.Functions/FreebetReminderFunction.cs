using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FreebetTracker.Functions;

public sealed class FreebetReminderFunction
{
    private readonly FreebetReminderRunner _runner;
    private readonly ILogger<FreebetReminderFunction> _logger;

    public FreebetReminderFunction(FreebetReminderRunner runner, ILogger<FreebetReminderFunction> logger)
    {
        _runner = runner;
        _logger = logger;
    }

    [Function(nameof(FreebetReminderFunction))]
    public async Task Run([TimerTrigger("0 30 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Freebet reminder timer triggered at {Time}.", DateTimeOffset.Now);

        try
        {
            await _runner.RunAsync();

            _logger.LogInformation("Freebet reminder runner completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Freebet reminder runner failed.");
            throw;
        }
    }
}
