using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;

namespace FreebetTracker.Tests.Services;

[TestClass]
public class FreebetServiceTests
{
    [TestMethod]
    public void SelectForReminder_WhenThreeUrgentFreebetsExist_ReturnsAllThreeUrgentFreebets()
    {
        var now = new DateTime(2026, 10, 17, 12, 0, 0);

        var freebets = new[]
        {
            CreateFreebet("1", now.AddDays(1)),
            CreateFreebet("2", now.AddDays(2)),
            CreateFreebet("3", now.AddDays(3)),
            CreateFreebet("4", now.AddDays(5))
        };

        var result = FreebetService.SelectForReminder(freebets, now);

        Assert.HasCount(3, result);
        Assert.IsFalse(result.Any(f => f.Id == "4"));
    }

    [TestMethod]
    public void SelectForReminder_WhenFewerThanThreeUrgentFreebetsExist_ReturnsFirstThreeEligibleFreebets()
    {
        var now = new DateTime(2026, 10, 17, 12, 0, 0);

        var freebets = new[]
        {
            CreateFreebet("1", now.AddDays(1)),
            CreateFreebet("2", now.AddDays(5)),
            CreateFreebet("3", now.AddDays(6)),
            CreateFreebet("4", now.AddDays(7))
        };

        var result = FreebetService.SelectForReminder(freebets, now);

        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, result.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public void SelectForReminder_WhenNowIsOmitted_UsesCurrentTime()
    {
        var before = DateTime.Now;

        var freebet = new Freebet
        {
            Id = "1",
            Player = "Testspiller",
            Account = "Testkonto",
            Amount = 100m,
            ExpiryDate = DateTime.Now.AddDays(1)
        };

        var result = FreebetService.SelectForReminder([freebet]);

        var after = DateTime.Now;

        Assert.HasCount(1, result);
        Assert.AreEqual("1", result[0].Id);
        Assert.IsTrue(freebet.ExpiryDate >= before);
        Assert.IsTrue(freebet.ExpiryDate >= after);
    }

    private static Freebet CreateFreebet(string id, DateTime expiryDate)
    {
        return new Freebet
        {
            Id = id,
            ExpiryDate = expiryDate,
            Account = "Testkonto"
        };
    }
}
