using FreebetTracker.Functions;

namespace FreebetTracker.Tests;

[TestClass]
public sealed class DanishReminderScheduleTests
{
    [TestMethod]
    public void ShouldRunAt_ShouldReturnTrueAt1230DuringWinterTime()
    {
        // 11:30 UTC = 12:30 dansk vintertid.
        var utcTime = new DateTimeOffset(2026, 1, 15, 11, 30, 0, TimeSpan.Zero);

        Assert.IsTrue(DanishReminderSchedule.ShouldRunAt(utcTime));
    }

    [TestMethod]
    public void ShouldRunAt_ShouldReturnTrueAt1230DuringSummerTime()
    {
        // 10:30 UTC = 12:30 dansk sommertid.
        var utcTime = new DateTimeOffset(2026, 7, 15, 10, 30, 0, TimeSpan.Zero);

        Assert.IsTrue(DanishReminderSchedule.ShouldRunAt(utcTime));
    }

    [TestMethod]
    public void ShouldRunAt_ShouldReturnFalseAt1159()
    {
        // 11:59 dansk sommertid.
        var utcTime = new DateTimeOffset(2026, 7, 15, 9, 59, 0, TimeSpan.Zero);

        Assert.IsFalse(DanishReminderSchedule.ShouldRunAt(utcTime));
    }

    [TestMethod]
    public void ShouldRunAt_ShouldReturnFalseAt1300()
    {
        // 13:00 dansk sommertid.
        var utcTime = new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero);

        Assert.IsFalse(DanishReminderSchedule.ShouldRunAt(utcTime));
    }
}
