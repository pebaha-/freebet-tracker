using FreebetTracker.Functions;

namespace FreebetTracker.Tests;

[TestClass]
public sealed class DanishReminderScheduleTests
{
    [TestMethod]
    public void ShouldRunAt_ShouldReturnTrueAtNoonDuringWinterTime()
    {
        // 11:00 UTC = 12:00 dansk vintertid.
        var utcTime = new DateTimeOffset(2026, 1, 15, 11, 0, 0, TimeSpan.Zero);

        Assert.IsTrue(DanishReminderSchedule.ShouldRunAt(utcTime));
    }

    [TestMethod]
    public void ShouldRunAt_ShouldReturnTrueAtNoonDuringSummerTime()
    {
        // 10:00 UTC = 12:00 dansk sommertid.
        var utcTime = new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero);

        Assert.IsTrue(DanishReminderSchedule.ShouldRunAt(utcTime));
    }

    [TestMethod]
    public void ShouldRunAt_ShouldReturnFalseOutsideNoonHour()
    {
        var utcTime = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);

        Assert.IsFalse(DanishReminderSchedule.ShouldRunAt(utcTime));
    }
}
