namespace FreebetTracker.Functions;

public static class DanishReminderSchedule
{
    private static readonly TimeZoneInfo DanishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    public static bool ShouldRunAt(DateTimeOffset utcTime)
    {
        var danishTime = TimeZoneInfo.ConvertTime(utcTime, DanishTimeZone);

        return danishTime.Hour == 12;
    }
}
