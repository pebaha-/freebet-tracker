using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FreebetTracker.Functions;

public sealed class FreebetReminderFunction(FreebetReminderRunner reminderRunner, ILogger<FreebetReminderFunction> logger)
{
    [Function("FreebetReminderFunction")]
    public async Task Run([TimerTrigger("0 30 * * * *")] TimerInfo timer, CancellationToken cancellationToken)
    {
        var utcTime = DateTimeOffset.UtcNow;
        var danishTime = TimeZoneInfo.ConvertTime(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen"));

        logger.LogInformation("Freebet reminder timer triggered at {DanishTime}.", danishTime);

        if (!DanishReminderSchedule.ShouldRunAt(utcTime))
        {
            return;
        }

        await reminderRunner.RunAsync(cancellationToken);
    }
}
