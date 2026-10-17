using FreebetTracker.Tests.Fakes;
using FreebetTracker.Web.Models;
using FreebetTracker.Web.Pages.Freebets;

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

        await model.OnGetAsync(CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "early", "middle", "late" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_WhenNoFreebetsExist_ReturnsEmptyList()
    {
        var repository = new FakeFreebetRepository();
        var model = new IndexModel(repository);

        await model.OnGetAsync(CancellationToken.None);

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

        await model.OnGetAsync(CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "late", "early" }, repository.Freebets.Select(f => f.Id).ToArray());
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
