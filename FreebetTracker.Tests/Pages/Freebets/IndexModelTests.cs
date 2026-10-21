using FreebetTracker.Tests.Fakes;
using FreebetTracker.Web.Models;
using FreebetTracker.Web.Pages.Freebets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

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

        await model.OnGetAsync("ExpiryDate", sortDescending: false, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "early", "middle", "late" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_WhenNoFreebetsExist_ReturnsEmptyList()
    {
        var repository = new FakeFreebetRepository();
        var model = new IndexModel(repository);

        await model.OnGetAsync("ExpiryDate", sortDescending: false, "All", 1, CancellationToken.None);

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

        await model.OnGetAsync("ExpiryDate", sortDescending: false, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "late", "early" }, repository.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnPostDeleteAsync_WhenFreebetExists_DeletesAndRedirects()
    {
        var repository = new FakeFreebetRepository();
        var freebet = CreateFreebet("test-id", DateTime.Today.AddDays(7));
        repository.Freebets.Add(freebet);
        var model = new IndexModel(repository);
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        model.TempData = tempData;

        var result = await model.OnPostDeleteAsync("test-id", "ExpiryDate", false, "All", 1, CancellationToken.None);

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

        var result = await model.OnPostDeleteAsync("missing-id", "ExpiryDate", false, "All", 1, CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundResult>(result);
        Assert.HasCount(0, repository.Freebets);
    }

    [TestMethod]
    public async Task OnGetAsync_SortsByAmountAscending()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("low", new DateTime(2026, 10, 18), amount: 10m),
            CreateFreebet("high", new DateTime(2026, 10, 19), amount: 300m),
            CreateFreebet("middle", new DateTime(2026, 10, 20), amount: 100m)
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("Amount", false, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[] { "low", "middle", "high" },
            model.Freebets.Select(f => f.Id).ToArray());
    }


    [TestMethod]
    public async Task OnGetAsync_SortsByAmountDescending()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("low", new DateTime(2026, 10, 18), amount: 10m),
            CreateFreebet("high", new DateTime(2026, 10, 19), amount: 300m),
            CreateFreebet("middle", new DateTime(2026, 10, 20), amount: 100m)
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("Amount", true, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "high", "middle", "low" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_SortsByAccountAscending()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("b", new DateTime(2026, 10, 18), account: "Bet365"),
            CreateFreebet("a", new DateTime(2026, 10, 19), account: "Betano"),
            CreateFreebet("c", new DateTime(2026, 10, 20), account: "Unibet")
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("Account", false, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "b", "a", "c" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_SortsByAccountDescending()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("b", new DateTime(2026, 10, 18), account: "Bet365"),
            CreateFreebet("a", new DateTime(2026, 10, 19), account: "Betano"),
            CreateFreebet("c", new DateTime(2026, 10, 20), account: "Unibet")
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("Account", true, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "c", "a", "b" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_SortsByPlayerAsscending()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("a", new DateTime(2026, 10, 18), player: "Anders"),
            CreateFreebet("c", new DateTime(2026, 10, 19), player: "Claus"),
            CreateFreebet("b", new DateTime(2026, 10, 20), player: "Bo")
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("Player", false, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_SortsByPlayerDescending()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("a", new DateTime(2026, 10, 18), player: "Anders"),
            CreateFreebet("c", new DateTime(2026, 10, 19), player: "Claus"),
            CreateFreebet("b", new DateTime(2026, 10, 20), player: "Bo")
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("Player", true, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "c", "b", "a" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_SortsByTermsAscending()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("z", new DateTime(2026, 10, 18), terms: "Omsætning"),
            CreateFreebet("a", new DateTime(2026, 10, 19), terms: "Ingen vilkår"),
            CreateFreebet("m", new DateTime(2026, 10, 20), terms: "Minimumsodds")
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("Terms", false, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "a", "m", "z" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_SortsByTermsDescending()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("z", new DateTime(2026, 10, 18), terms: "Omsætning"),
            CreateFreebet("a", new DateTime(2026, 10, 19), terms: "Ingen vilkår"),
            CreateFreebet("m", new DateTime(2026, 10, 20), terms: "Minimumsodds")
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("Terms", true, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "z", "m", "a" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_SortsByTermsAscending_WhenSomeTermsAreNull()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("with-terms", new DateTime(2026, 10, 18), terms: "Omsætning"),
            CreateFreebet("without-terms", new DateTime(2026, 10, 19), terms: null),
            CreateFreebet("other-terms", new DateTime(2026, 10, 20), terms: "Minimumsodds")
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("Terms", false, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "without-terms", "other-terms", "with-terms" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_SortsByIsUsedAscending()
    {
        var repository = new FakeFreebetRepository();
        var unused = CreateFreebet("unused", new DateTime(2026, 10, 18));
        var used = CreateFreebet("used", new DateTime(2026, 10, 19));
        used.IsUsed = true;

        repository.Freebets.AddRange([used, unused]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("IsUsed", false, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "unused", "used" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_SortsByIsUsedDescending()
    {
        var repository = new FakeFreebetRepository();
        var unused = CreateFreebet("unused", new DateTime(2026, 10, 18));
        var used = CreateFreebet("used", new DateTime(2026, 10, 19));
        used.IsUsed = true;

        repository.Freebets.AddRange([used, unused]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("IsUsed", true, "All", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "used", "unused" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_UnknownSortColumn_FallsBackToExpiryDate()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("late", new DateTime(2026, 10, 25)),
            CreateFreebet("early", new DateTime(2026, 10, 18))
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("UnknownColumn", false, "All", 1, CancellationToken.None);

        Assert.AreEqual("ExpiryDate", model.SortBy);
        CollectionAssert.AreEqual(new[] { "early", "late" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_StatusFilterUnused_ReturnsOnlyUnusedFreebets()
    {
        var repository = new FakeFreebetRepository();
        var unused = CreateFreebet("unused", new DateTime(2026, 10, 18));
        var used = CreateFreebet("used", new DateTime(2026, 10, 19));
        used.IsUsed = true;

        repository.Freebets.AddRange([used, unused]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("ExpiryDate", false, "Unused", 1, CancellationToken.None);

        Assert.AreEqual("Unused", model.StatusFilter);
        CollectionAssert.AreEqual(new[] { "unused" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_StatusFilterUsed_ReturnsOnlyUsedFreebets()
    {
        var repository = new FakeFreebetRepository();
        var unused = CreateFreebet("unused", new DateTime(2026, 10, 18));
        var used = CreateFreebet("used", new DateTime(2026, 10, 19));
        used.IsUsed = true;

        repository.Freebets.AddRange([unused, used]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("ExpiryDate", false, "Used", 1, CancellationToken.None);

        Assert.AreEqual("Used", model.StatusFilter);
        CollectionAssert.AreEqual(new[] { "used" }, model.Freebets.Select(f => f.Id).ToArray());
    }

    [TestMethod]
    public async Task OnGetAsync_StatusFilterIsCaseInsensitive()
    {
        var repository = new FakeFreebetRepository();
        repository.Freebets.AddRange(
        [
            CreateFreebet("unused", new DateTime(2026, 10, 18))
        ]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("ExpiryDate", false, "UNUSED", 1, CancellationToken.None);

        Assert.AreEqual("Unused", model.StatusFilter);
        Assert.HasCount(1, model.Freebets);
    }

    [TestMethod]
    public async Task OnGetAsync_UnknownStatusFilter_DefaultsToReturningUnusedFreebets()
    {
        var repository = new FakeFreebetRepository();
        var unused = CreateFreebet("unused", new DateTime(2026, 10, 18));
        var used = CreateFreebet("used", new DateTime(2026, 10, 19));
        used.IsUsed = true;

        repository.Freebets.AddRange([unused, used]);

        var model = new IndexModel(repository);

        await model.OnGetAsync("ExpiryDate", false, "invalid", 1, CancellationToken.None);

        Assert.AreEqual("Unused", model.StatusFilter);
        Assert.HasCount(1, model.Freebets);
    }

    [TestMethod]
    public void NextSortDescending_SameColumnAscending_ReturnsTrue()
    {
        var model = new IndexModel(new FakeFreebetRepository());

        Assert.IsTrue(model.NextSortDescending("ExpiryDate"));
    }

    [TestMethod]
    public async Task NextSortDescending_SameColumnDescending_ReturnsFalse()
    {
        var model = new IndexModel(new FakeFreebetRepository());

        await model.OnGetAsync("Amount", true, "All", 1, CancellationToken.None);

        Assert.IsFalse(model.NextSortDescending("Amount"));
    }

    [TestMethod]
    public async Task NextSortDescending_DifferentColumn_ReturnsFalse()
    {
        var model = new IndexModel(new FakeFreebetRepository());

        await model.OnGetAsync("Amount", true, "All", 1, CancellationToken.None);

        Assert.IsFalse(model.NextSortDescending("Account"));
    }

    [TestMethod]
    public async Task SortIndicator_ReturnsCorrectArrow()
    {
        var model = new IndexModel(new FakeFreebetRepository());

        await model.OnGetAsync("Amount", false, "All", 1, CancellationToken.None);
        Assert.AreEqual(" ▲", model.SortIndicator("Amount"));
        Assert.AreEqual(string.Empty, model.SortIndicator("Account"));

        await model.OnGetAsync("Amount", true, "All", 1, CancellationToken.None);
        Assert.AreEqual(" ▼", model.SortIndicator("Amount"));
    }

    [TestMethod]
    public async Task OnPostMarkAsUsedAsync_WhenFreebetExists_MarksAsUsedAndRedirects()
    {
        var repository = new FakeFreebetRepository();
        var freebet = CreateFreebet("test-id", new DateTime(2026, 10, 25));
        repository.Freebets.Add(freebet);

        var model = new IndexModel(repository);
        var result = await model.OnPostMarkAsUsedAsync("test-id", "Amount", true, "All", 1, CancellationToken.None);

        Assert.IsInstanceOfType<RedirectToPageResult>(result);

        var redirect = (RedirectToPageResult)result;
        Assert.AreEqual("./Index", redirect.PageName);
        Assert.IsTrue(freebet.IsUsed);

        Assert.IsNotNull(redirect.RouteValues);
        Assert.AreEqual("Amount", redirect.RouteValues["sortBy"]);
        Assert.IsTrue(redirect.RouteValues["sortDescending"]?.Equals(true));
    }

    [TestMethod]
    public async Task OnPostMarkAsUsedAsync_WhenFreebetDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeFreebetRepository();
        var model = new IndexModel(repository);
        var result = await model.OnPostMarkAsUsedAsync("missing-id", "ExpiryDate", false, "All", 1, CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    private static Freebet CreateFreebet(string id, DateTime expiryDate, decimal amount = 100m, string player = "Test player", string account = "Test account", string? terms = null)
    {
        return new Freebet
        {
            Id = id,
            Player = player,
            Account = account,
            Amount = amount,
            ExpiryDate = expiryDate,
            ExpiryTimeSpecified = false,
            Terms = terms,
            IsUsed = false
        };
    }
}
