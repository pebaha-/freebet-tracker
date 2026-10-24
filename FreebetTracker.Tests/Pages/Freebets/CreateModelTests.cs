using FreebetTracker.Tests.Fakes;
using FreebetTracker.Web.Models;
using FreebetTracker.Web.Pages.Freebets;
using FreebetTracker.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace FreebetTracker.Tests.Pages.Freebets;

[TestClass]
public sealed class CreateModelTests
{
    private FakeFreebetRepository _repository = null!;
    private CreateModel _model = null!;

    [TestInitialize]
    public void Initialize()
    {
        _repository = new FakeFreebetRepository();
        var suggestionService = new FreebetSuggestionService(_repository);
        _model = new CreateModel(_repository, suggestionService);
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        _model.TempData = tempData;
    }

    [TestMethod]
    public async Task OnPostAsync_WithValidInput_CreatesFreebetAndRedirects()
    {
        SetValidInput();

        var result = await _model.OnPostAsync(CancellationToken.None);

        Assert.HasCount(1, _repository.Freebets);

        var freebet = _repository.Freebets[0];
        Assert.AreEqual("Peter", freebet.Player);
        Assert.AreEqual("Bet365", freebet.Account);
        Assert.AreEqual(125.50m, freebet.Amount);

        var redirect = result as RedirectToPageResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("./Index", redirect.PageName);
    }

    [TestMethod]
    public async Task OnPostAsync_TrimsTextFields()
    {
        SetValidInput();
        _model.Input.Player = "  Peter  ";
        _model.Input.Account = "  Bet365  ";
        _model.Input.Terms = "  Minimum odds  ";

        await _model.OnPostAsync(CancellationToken.None);

        var freebet = _repository.Freebets.Single();
        Assert.AreEqual("Peter", freebet.Player);
        Assert.AreEqual("Bet365", freebet.Account);
        Assert.AreEqual("Minimum odds", freebet.Terms);
    }

    [TestMethod]
    public async Task OnPostAsync_WithoutExpiryTime_UsesEndOfDay()
    {
        SetValidInput();
        _model.Input.ExpiryDate = new DateTime(2026, 10, 20);
        _model.Input.ExpiryTime = null;

        await _model.OnPostAsync(CancellationToken.None);

        var freebet = _repository.Freebets.Single();
        Assert.AreEqual(new DateTime(2026, 10, 20).AddDays(1).AddTicks(-1), freebet.ExpiryDate);
        Assert.IsFalse(freebet.ExpiryTimeSpecified);
    }

    [TestMethod]
    public void ToFreebet_WithoutExpiryTime_UsesEndOfDay()
    {
        SetValidInput();

        _model.Input.ExpiryDate = new DateTime(2026, 10, 20);
        _model.Input.ExpiryTime = null;

        var freebet = _model.Input.ToFreebet();

        Assert.AreEqual(new DateTime(2026, 10, 20).AddDays(1).AddTicks(-1), freebet.ExpiryDate);
        Assert.IsFalse(freebet.ExpiryTimeSpecified);
    }

    [TestMethod]
    public async Task OnPostAsync_WithExpiryTime_CombinesDateAndTime()
    {
        SetValidInput();
        _model.Input.ExpiryDate = new DateTime(2026, 10, 20);
        _model.Input.ExpiryTime = new TimeOnly(18, 30);

        await _model.OnPostAsync(CancellationToken.None);

        var freebet = _repository.Freebets.Single();
        Assert.AreEqual(new DateTime(2026, 10, 20, 18, 30, 0), freebet.ExpiryDate);
        Assert.IsTrue(freebet.ExpiryTimeSpecified);
    }

    [TestMethod]
    public async Task OnPostAsync_WithBlankTerms_StoresNull()
    {
        SetValidInput();
        _model.Input.Terms = "   ";

        await _model.OnPostAsync(CancellationToken.None);

        Assert.IsNull(_repository.Freebets.Single().Terms);
    }

    [TestMethod]
    public async Task OnPostAsync_WithInvalidModelState_DoesNotCreateFreebet()
    {
        SetValidInput();
        _model.ModelState.AddModelError(nameof(CreateModel.Input), "Invalid input");

        var result = await _model.OnPostAsync(CancellationToken.None);

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.HasCount(0, _repository.Freebets);
    }

    [TestMethod]
    public async Task OnGet_SetsExpiryDateToToday()
    {
        await _model.OnGetAsync();

        Assert.AreEqual(DateTime.Today, _model.Input.ExpiryDate);
    }

    private void SetValidInput()
    {
        _model.Input = new FreebetInputModel
        {
            Player = "Peter",
            Account = "Bet365",
            Amount = 125.50m,
            ExpiryDate = new DateTime(2026, 10, 20),
            ExpiryTime = null,
            Terms = "Minimum odds"
        };
    }
}
