using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;

namespace FreebetTracker.Tests.Services;

[TestClass]
public class CasinoBalanceServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(new(2026, 10, 21));

    [TestMethod]
    public void SelectForReminder_NeverChecked_IncludesAccount()
    {
        var account = CreateAccount(lastChecked: null);
        var result = CasinoBalanceService.SelectForReminder([account], Today);

        Assert.HasCount(1, result);
        Assert.AreSame(account, result[0]);
    }

    [TestMethod]
    public void SelectForReminder_Checked29DaysAgo_ExcludesAccount()
    {
        var account = CreateAccount(lastChecked: Today.AddDays(-29));
        var result = CasinoBalanceService.SelectForReminder([account], Today);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void SelectForReminder_CheckedExactly30DaysAgo_IncludesAccount()
    {
        var account = CreateAccount(lastChecked: Today.AddDays(-30));
        var result = CasinoBalanceService.SelectForReminder([account], Today);

        Assert.HasCount(1, result);
        Assert.AreSame(account, result[0]);
    }

    [TestMethod]
    public void SelectForReminder_CheckedMoreThan30DaysAgo_IncludesAccount()
    {
        var account = CreateAccount(lastChecked: Today.AddDays(-40));
        var result = CasinoBalanceService.SelectForReminder([account], Today);

        Assert.HasCount(1, result);
        Assert.AreSame(account, result[0]);
    }

    [TestMethod]
    public void SelectForReminder_ReminderAlreadySent_ExcludesAccount()
    {
        var account = CreateAccount(lastChecked: Today.AddDays(-40), reminderSent: Today.AddDays(-2));
        var result = CasinoBalanceService.SelectForReminder([account], Today);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void SelectForReminder_EmptyProvider_ExcludesAccount()
    {
        var account = CreateAccount(lastChecked: null, provider: "");
        var result = CasinoBalanceService.SelectForReminder([account], Today);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void SelectForReminder_EmptyList_ReturnsEmptyList()
    {
        var result = CasinoBalanceService.SelectForReminder([], Today);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void SelectForReminder_SortsNeverCheckedAccountsFirst()
    {
        var recentlyChecked = CreateAccount(provider: "Provider A", lastChecked: Today.AddDays(-35));
        var neverChecked = CreateAccount(provider: "Provider B", lastChecked: null);
        var result = CasinoBalanceService.SelectForReminder([recentlyChecked, neverChecked], Today);

        Assert.HasCount(2, result);
        Assert.AreSame(neverChecked, result[0]);
        Assert.AreSame(recentlyChecked, result[1]);
    }

    [TestMethod]
    public void GetStatus_NeverChecked_ReturnsMissingCheck()
    {
        var account = CreateAccount(lastChecked: null);
        var result = CasinoBalanceService.GetStatus(account, Today);

        Assert.AreEqual("Mangler tjek", result);
    }

    [TestMethod]
    public void GetStatus_Checked29DaysAgo_ReturnsOk()
    {
        var account = CreateAccount(lastChecked: Today.AddDays(-29));
        var result = CasinoBalanceService.GetStatus(account, Today);

        Assert.AreEqual("OK", result);
    }

    [TestMethod]
    public void GetStatus_CheckedExactly30DaysAgo_ReturnsOverdue()
    {
        var account = CreateAccount(lastChecked: Today.AddDays(-30));
        var result = CasinoBalanceService.GetStatus(account, Today);

        Assert.AreEqual("Forfalden", result);
    }

    [TestMethod]
    public void GetStatus_EmptyProvider_ReturnsEmptyStatus()
    {
        var account = CreateAccount(provider: "", lastChecked: Today.AddDays(-40));
        var result = CasinoBalanceService.GetStatus(account, Today);

        Assert.AreEqual("", result);
    }

    private static CasinoBalance CreateAccount(string provider = "Test Provider", string player = "Test Player", DateOnly? lastChecked = null, DateOnly? reminderSent = null)
    {
        return new CasinoBalance
        {
            Provider = provider,
            Player = player,
            Balance = 100m,
            LastChecked = lastChecked,
            ReminderSent = reminderSent
        };
    }
}
