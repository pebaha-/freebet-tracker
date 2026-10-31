namespace FreebetTracker.Functions;

public static class DanishReminderSchedule
{
    private static readonly TimeZoneInfo DanishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    public static bool ShouldRunAt(DateTimeOffset utcTime)
    {
        var danishTime = TimeZoneInfo.ConvertTime(utcTime, DanishTimeZone);
        var reminderHour = int.TryParse(Environment.GetEnvironmentVariable("FreebetReminderHour"), out var hour) ? hour : 12;

        return danishTime.Hour == reminderHour;
    }
}
