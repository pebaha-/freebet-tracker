using FreebetTracker.Tests.Fakes;
using FreebetTracker.Web.Models;
using FreebetTracker.Web.Pages.Freebets;
using Microsoft.AspNetCore.Mvc;

namespace FreebetTracker.Tests.Pages.Freebets;

[TestClass]
public sealed class IndexModelTests
{
    [TestMethod]
    public async Task OnGetAsync_SortsFreebetsByExpiryDateAscending()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("late", new DateTime(2026, 10, 25)),
            CreateFreebet("early", new DateTime(2026, 10, 18)),
            CreateFreebet("middle", new DateTime(2026, 10, 21))
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("ExpiryDate", sortDescending: false, "All", CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "early", "middle", "late" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_WhenNoFreebetsExist_ReturnsEmptyList()
    {
        var repository = new FakeFreebetRepository();
        var model = new IndexModel(repository);

        await model.OnGetAsync("ExpiryDate", sortDescending: false, "All", CancellationToken.None);

        Assert.IsNotNull(model.Freebets);
        Assert.HasCount(0, model.Freebets);
    }

    [TestMethod]
    public async Task OnGetAsync_DoesNotModifyRepositoryOrder()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("late", new DateTime(2026, 10, 25)),
            CreateFreebet("early", new DateTime(2026, 10, 18))
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("ExpiryDate", sortDescending: false, "All", CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "late", "early" }, repository.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnPostDeleteAsync_WhenFreebetExists_DeletesAndRedirects()
    {
        var repository = new FakeFreebetRepository();
        var freebet = CreateFreebet("test-id", DateTime.Today.AddDays(7));
        repository.Freebets.Add(freebet);
        var model = new IndexModel(repository);

        var result = await model.OnPostDeleteAsync("test-id", CancellationToken.None);

        Assert.IsInstanceOfType<RedirectToPageResult>(result);

        var redirect = (RedirectToPageResult)result;
        Assert.AreEqual("./Index", redirect.PageName);

        Assert.IsFalse(repository.Freebets.Any(f => f.Id == "test-id"));
    }

    [TestMethod]
    public async Task OnPostDeleteAsync_WhenFreebetDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeFreebetRepository();
        var model = new IndexModel(repository);

        var result = await model.OnPostDeleteAsync("missing-id", CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundResult>(result);
        Assert.HasCount(0, repository.Freebets);
    }

    private static Freebet CreateFreebet(string id, DateTime expiryDate)
    {
        return new Freebet
        {
            Id = id,
            Player = "Test player",
            Account = "Test account",
            Amount = 100m,
            ExpiryDate = expiryDate,
            ExpiryTimeSpecified = false,
            ReminderSent = false
        };
    }
}
