using FreebetTracker.Web.Models;

namespace FreebetTracker.Web.Services;

public class CasinoBalanceService : ICasinoBalanceService
{
    public const int DaysUntilReminder = 30;

    public static IReadOnlyList<CasinoBalance> SelectForReminder(IEnumerable<CasinoBalance> accounts, DateOnly? today = null)
    {
        var currentDate = today ?? DateOnly.FromDateTime(DateTime.Today);

        return accounts
            .Where(account => !string.IsNullOrWhiteSpace(account.Provider))
            .Where(account => account.ReminderSent is null)
            .Where(account =>
                account.LastChecked is null ||
                DaysSinceCheck(account.LastChecked.Value, currentDate)
                    >= DaysUntilReminder)
            .OrderBy(account => account.LastChecked)
            .ThenBy(account => account.Provider)
            .ThenBy(account => account.Player)
            .ToList();
    }

    public static string GetStatus(CasinoBalance account, DateOnly? today = null)
    {
        if (string.IsNullOrWhiteSpace(account.Provider))
        {
            return string.Empty;
        }

        if (account.LastChecked is null)
        {
            return "Mangler tjek";
        }

        var currentDate = today ?? DateOnly.FromDateTime(DateTime.Today);
        var daysSinceCheck = DaysSinceCheck(account.LastChecked.Value, currentDate);

        return daysSinceCheck >= DaysUntilReminder
            ? "Forfalden"
            : "OK";
    }

    private static int DaysSinceCheck(DateOnly lastChecked, DateOnly currentDate)
    {
        return currentDate.DayNumber - lastChecked.DayNumber;
    }
}
