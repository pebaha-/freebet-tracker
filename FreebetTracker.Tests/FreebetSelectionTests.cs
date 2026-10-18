using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;

namespace FreebetTracker.Tests;

[TestClass]
public sealed class FreebetSelectionTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0);

    private static Freebet CreateFreebet(string id, string account, int daysUntilExpiry, bool isUsed = false)
    {
        return new Freebet
        {
            Id = id,
            Account = account,
            ExpiryDate = Now.AddDays(daysUntilExpiry),
            IsUsed = isUsed
        };
    }

    [TestMethod]
    public void Should_Select_All_When_Fewer_Than_Three_Eligible()
    {
        var freebets = new[]
        {
            CreateFreebet("1", "Bet365", 1),
            CreateFreebet("2", "Unibet", 2)
        };

        var result = FreebetService.SelectForReminder(freebets, Now);

        CollectionAssert.AreEqual(new string[] { "1", "2" }, result.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public void Should_Select_All_Urgent_When_At_Least_Three_Are_Urgent()
    {
        var freebets = new[]
        {
            CreateFreebet("1", "Bet365", 1),
            CreateFreebet("2", "Unibet", 2),
            CreateFreebet("3", "Betano", 3),
            CreateFreebet("4", "LeoVegas", 5)
        };

        var result = FreebetService.SelectForReminder(freebets, Now);

        CollectionAssert.AreEqual(new string[] { "1", "2", "3" }, result.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public void Should_Select_More_Than_Three_When_All_Are_Urgent()
    {
        var freebets = new[]
        {
            CreateFreebet("1", "Bet365", 1),
            CreateFreebet("2", "Unibet", 2),
            CreateFreebet("3", "Betano", 2),
            CreateFreebet("4", "LeoVegas", 3)
        };

        var result = FreebetService.SelectForReminder(freebets, Now);

        CollectionAssert.AreEqual(new string[] { "1", "3", "2", "4" }, result.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public void Should_Supplement_Urgent_Freebets_With_Earliest_Expiry()
    {
        var freebets = new[]
        {
            CreateFreebet("1", "Bet365", 2),
            CreateFreebet("2", "Unibet", 5),
            CreateFreebet("3", "Betano", 4),
            CreateFreebet("4", "LeoVegas", 8)
        };

        var result = FreebetService.SelectForReminder(freebets, Now);

        CollectionAssert.AreEqual(new string[] { "1", "3", "2" }, result.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public void Should_Exclude_Already_Reminded_Freebets()
    {
        var freebets = new[]
        {
            CreateFreebet("1", "Bet365", 1, isUsed: true),
            CreateFreebet("2", "Unibet", 2)
        };

        var result = FreebetService.SelectForReminder(freebets, Now);

        CollectionAssert.AreEqual(
            new[] { "2" },
            result.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public void Should_Exclude_Expired_Freebets()
    {
        var freebets = new[]
        {
            CreateFreebet("1", "Bet365", -1),
            CreateFreebet("2", "Unibet", 1)
        };

        var result = FreebetService.SelectForReminder(freebets, Now);

        CollectionAssert.AreEqual(
            new[] { "2" },
            result.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public void Should_Return_Empty_When_No_Freebets_Are_Eligible()
    {
        var freebets = new[]
        {
            CreateFreebet("1", "Bet365", -1),
            CreateFreebet("2", "Unibet", 1, isUsed: true)
        };

        var result = FreebetService.SelectForReminder(freebets, Now);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void Should_Sort_By_Account_When_Expiry_Dates_Are_Equal()
    {
        var freebets = new[]
        {
            CreateFreebet("1", "Unibet", 1),
            CreateFreebet("2", "Bet365", 1),
            CreateFreebet("3", "Betano", 1)
        };

        var result = FreebetService.SelectForReminder(freebets, Now);

        CollectionAssert.AreEqual(new string[] { "2", "3", "1" }, result.Select(f => f.Id).ToArray());
    }
}
