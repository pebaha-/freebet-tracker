using FreebetTracker.Tests.Fakes;
using FreebetTracker.Web.Models;
using FreebetTracker.Web.Pages.Freebets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace FreebetTracker.Tests.Pages.Freebets;

[TestClass]
public sealed class EditModelTests
{
    private FakeFreebetRepository _repository = null!;
    private EditModel _model = null!;

    [TestInitialize]
    public void Initialize()
    {
        _repository = new FakeFreebetRepository();
        _model = new EditModel(_repository);

        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        _model.TempData = tempData;
    }

    [TestMethod]
    public async Task OnGetAsync_WhenFreebetExists_LoadsInputAndReturnsPage()
    {
        var freebet = CreateFreebet();
        await _repository.AddAsync(freebet);

        var result = await _model.OnGetAsync(freebet.Id, CancellationToken.None);

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.AreEqual(freebet.Id, _model.Input.Id);
        Assert.AreEqual(freebet.Player, _model.Input.Player);
        Assert.AreEqual(freebet.Account, _model.Input.Account);
        Assert.AreEqual(freebet.Amount, _model.Input.Amount);
        Assert.AreEqual(freebet.ExpiryDate.Date, _model.Input.ExpiryDate);
        Assert.AreEqual(new TimeOnly(18, 30), _model.Input.ExpiryTime);
        Assert.AreEqual(freebet.Terms, _model.Input.Terms);
    }

    [TestMethod]
    public async Task OnGetAsync_WhenFreebetDoesNotExist_ReturnsNotFound()
    {
        var result = await _model.OnGetAsync("missing-id", CancellationToken.None);
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task OnGetAsync_WhenExpiryTimeWasNotSpecified_LeavesTimeNull()
    {
        var freebet = CreateFreebet();
        freebet.ExpiryTimeSpecified = false;
        freebet.ExpiryDate = new DateTime(2026, 10, 20).AddDays(1).AddTicks(-1);

        await _repository.AddAsync(freebet);

        await _model.OnGetAsync(freebet.Id, CancellationToken.None);

        Assert.IsNull(_model.Input.ExpiryTime);
        Assert.AreEqual(new DateTime(2026, 10, 20), _model.Input.ExpiryDate);
    }

    [TestMethod]
    public async Task OnPostAsync_WithValidInput_UpdatesFreebetAndRedirects()
    {
        var existing = CreateFreebet();
        await _repository.AddAsync(existing);

        SetValidInput(existing.Id);

        var result = await _model.OnPostAsync(CancellationToken.None);

        var updated = _repository.Freebets.Single();

        Assert.AreEqual("Updated player", updated.Player);
        Assert.AreEqual("Updated account", updated.Account);
        Assert.AreEqual(250m, updated.Amount);
        Assert.AreEqual("Updated terms", updated.Terms);

        var redirect = result as RedirectToPageResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("./Index", redirect.PageName);
    }

    [TestMethod]
    public async Task OnPostAsync_WhenExpiryTimeIsRemoved_UsesEndOfDay()
    {
        var existing = CreateFreebet();
        await _repository.AddAsync(existing);

        SetValidInput(existing.Id);
        _model.Input.ExpiryDate = new DateTime(2026, 10, 21);
        _model.Input.ExpiryTime = null;

        await _model.OnPostAsync(CancellationToken.None);

        var updated = _repository.Freebets.Single();
        Assert.IsFalse(updated.ExpiryTimeSpecified);
        Assert.AreEqual(new DateTime(2026, 10, 21).AddDays(1).AddTicks(-1), updated.ExpiryDate);
    }

    [TestMethod]
    public async Task OnPostAsync_WhenFreebetDoesNotExist_ReturnsNotFound()
    {
        SetValidInput("missing-id");

        var result = await _model.OnPostAsync(CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundResult>(result);
        Assert.HasCount(0, _repository.Freebets);
    }

    [TestMethod]
    public async Task OnPostAsync_WithInvalidModelState_DoesNotUpdateFreebet()
    {
        var existing = CreateFreebet();
        await _repository.AddAsync(existing);

        SetValidInput(existing.Id);
        _model.ModelState.AddModelError("Input.Player", "Invalid player");

        var result = await _model.OnPostAsync(CancellationToken.None);

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.AreEqual("Test player", _repository.Freebets.Single().Player);
    }

    [TestMethod]
    public async Task OnPostAsync_WithBlankTerms_StoresNull()
    {
        var existing = CreateFreebet();
        await _repository.AddAsync(existing);

        SetValidInput(existing.Id);
        _model.Input.Terms = "   ";

        await _model.OnPostAsync(CancellationToken.None);

        Assert.IsNull(_repository.Freebets.Single().Terms);
    }

    private void SetValidInput(string id)
    {
        _model.Input = new EditModel.InputModel
        {
            Id = id,
            Player = "Updated player",
            Account = "Updated account",
            Amount = 250m,
            ExpiryDate = new DateTime(2026, 10, 21),
            ExpiryTime = new TimeOnly(12, 45),
            Terms = "Updated terms"
        };
    }

    private static Freebet CreateFreebet()
    {
        return new Freebet
        {
            Id = "test-id",
            Player = "Test player",
            Account = "Test account",
            Amount = 125.50m,
            ExpiryDate = new DateTime(2026, 10, 20, 18, 30, 0),
            ExpiryTimeSpecified = true,
            Terms = "Test terms"
        };
    }
}
