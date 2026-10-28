using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FreebetTracker.Web.Pages.Freebets;

public class IndexModel(IFreebetRepository repository, IReminderPauseStore reminderPauseStore) : PageModel
{
    private const int PageSize = 10;

    public IReadOnlyList<Freebet> Freebets { get; private set; } = [];

    public IReadOnlyList<string> AccountOptions { get; private set; } = [];

    public IReadOnlyList<string> PlayerOptions { get; private set; } = [];

    public string StatusFilter { get; private set; } = "All";

    public string AccountFilter { get; private set; } = "All";

    public string PlayerFilter { get; private set; } = "All";

    public string SortBy { get; private set; } = "ExpiryDate";

    public bool SortDescending { get; private set; }

    public int PageNumber { get; private set; } = 1;

    public int TotalPages { get; private set; } = 1;

    public int FilteredCount { get; private set; }

    public int TotalCount { get; private set; }

    public DateTimeOffset? PausedUntilUtc { get; private set; }

    public bool IsReminderPauseActive =>
        PausedUntilUtc is not null &&
        PausedUntilUtc > DateTimeOffset.UtcNow;

    private static readonly TimeZoneInfo BusinessTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "Romance Standard Time"
                : "Europe/Copenhagen");

    public DateTime TomorrowInBusinessTimeZone =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, BusinessTimeZone)
            .Date
            .AddDays(1);

    [BindProperty]
    public string? ResumeDate { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public bool NextSortDescending(string column)
    {
        return string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase) && !SortDescending;
    }

    public string SortIndicator(string column)
    {
        if (!string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return SortDescending ? " ▼" : " ▲";
    }


    public async Task OnGetAsync(
        string? sortBy,
        bool sortDescending,
        string? statusFilter,
        string? accountFilter,
        string? playerFilter,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        PausedUntilUtc = await reminderPauseStore.GetPausedUntilAsync(cancellationToken);

        StatusFilter = statusFilter?.ToLowerInvariant() switch
        {
            "all" => "All",
            "used" => "Used",
            "unused" => "Unused",
            _ => "Unused"
        };

        SortBy = sortBy?.ToLowerInvariant() switch
        {
            "account" => "Account",
            "player" => "Player",
            "amount" => "Amount",
            "expirydate" => "ExpiryDate",
            "terms" => "Terms",
            "isused" => "IsUsed",
            _ => "ExpiryDate"
        };

        SortDescending = sortDescending;

        var freebets = (await repository.GetAllAsync(cancellationToken)).ToList();

        TotalCount = freebets.Count;

        // 1. Filtrér efter status først.
        IEnumerable<Freebet> statusFilteredFreebets = StatusFilter switch
        {
            "Unused" => freebets.Where(f => !f.IsUsed),
            "Used" => freebets.Where(f => f.IsUsed),
            _ => freebets
        };

        var statusFilteredList = statusFilteredFreebets.ToList();

        // 2. Byg Konto- og Spiller-dropdowns ud fra statusfiltrerede freebets.
        //    Filtrene påvirker ikke hinandens valgmuligheder.
        AccountOptions = statusFilteredList
            .Select(f => f.Account)
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
            .ToList();

        PlayerOptions = statusFilteredList
            .Select(f => f.Player)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 3. Normalisér de valgte filtre mod de aktuelle muligheder.
        //    Hvis et filter ikke længere findes efter statusskiftet,
        //    falder det tilbage til "All".
        AccountFilter = NormalizeFilter(accountFilter, AccountOptions);
        PlayerFilter = NormalizeFilter(playerFilter, PlayerOptions);

        // 4. Anvend Konto- og Spiller-filtrene.
        IEnumerable<Freebet> filteredFreebets = statusFilteredList;

        if (AccountFilter != "All")
        {
            filteredFreebets = filteredFreebets.Where(f =>
                string.Equals(f.Account, AccountFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (PlayerFilter != "All")
        {
            filteredFreebets = filteredFreebets.Where(f =>
                string.Equals(f.Player, PlayerFilter, StringComparison.OrdinalIgnoreCase));
        }

        // 5. Sortér som hidtil.
        var sorted = SortBy switch
        {
            "Account" => SortDescending
                ? filteredFreebets.OrderByDescending(f => f.Account)
                : filteredFreebets.OrderBy(f => f.Account),

            "Player" => SortDescending
                ? filteredFreebets.OrderByDescending(f => f.Player)
                : filteredFreebets.OrderBy(f => f.Player),

            "Amount" => SortDescending
                ? filteredFreebets.OrderByDescending(f => f.Amount)
                : filteredFreebets.OrderBy(f => f.Amount),

            "ExpiryDate" => SortDescending
                ? filteredFreebets.OrderByDescending(f => f.ExpiryDate)
                : filteredFreebets.OrderBy(f => f.ExpiryDate),

            "Terms" => SortDescending
                ? filteredFreebets.OrderByDescending(f => f.Terms)
                : filteredFreebets.OrderBy(f => f.Terms),

            "IsUsed" => SortDescending
                ? filteredFreebets.OrderByDescending(f => f.IsUsed)
                : filteredFreebets.OrderBy(f => f.IsUsed),

            _ => filteredFreebets.OrderBy(f => f.ExpiryDate)
        };

        var sortedList = sorted.ToList();

        // 6. Paginering.
        FilteredCount = sortedList.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
        PageNumber = Math.Clamp(pageNumber, 1, TotalPages);

        Freebets =
        [
            .. sortedList
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
        ];
    }

    private static string NormalizeFilter(string? filter, IReadOnlyList<string> options)
    {
        if (string.IsNullOrWhiteSpace(filter) || string.Equals(filter, "All", StringComparison.OrdinalIgnoreCase))
        {
            return "All";
        }

        return options.FirstOrDefault(option => string.Equals(option, filter, StringComparison.OrdinalIgnoreCase)) ?? "All";
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        string id,
        string? sortBy,
        bool sortDescending,
        string? statusFilter,
        string? accountFilter,
        string? playerFilter,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByIdAsync(id, cancellationToken);

        if (existing is null)
        {
            return NotFound();
        }

        await repository.DeleteAsync(existing, cancellationToken);

        TempData["SuccessMessage"] = "Freebet er slettet";

        return RedirectToPage("./Index", new
        {
            sortBy,
            sortDescending,
            statusFilter,
            accountFilter,
            playerFilter,
            pageNumber
        });
    }

    public async Task<IActionResult> OnPostMarkAsUsedAsync(
        string id,
        string? sortBy,
        bool sortDescending,
        string? statusFilter,
        string? accountFilter,
        string? playerFilter,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var freebet = await repository.GetByIdAsync(id, cancellationToken);

        if (freebet is null)
        {
            return NotFound();
        }

        freebet.IsUsed = true;
        await repository.UpdateAsync(freebet, cancellationToken);
        await Task.Delay(1500, cancellationToken);

        SuccessMessage = "Freebettet er markeret som brugt.";

        return RedirectToPage("./Index", new
        {
            sortBy,
            sortDescending,
            statusFilter,
            accountFilter,
            playerFilter,
            pageNumber
        });
    }

    public async Task<IActionResult> OnPostSetReminderPauseAsync(CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParseExact(ResumeDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var resumeDate))
        {
            TempData["ErrorMessage"] = "Vælg en gyldig genoptagelsesdato.";
            return RedirectToPage("./Index");
        }

        var resumeTimestampUtc = new DateTimeOffset(resumeDate.Year, resumeDate.Month, resumeDate.Day, 0, 0, 0, TimeSpan.Zero);

        if (resumeTimestampUtc <= DateTimeOffset.UtcNow)
        {
            TempData["ErrorMessage"] = "Genoptagelsesdatoen skal være i fremtiden.";
            return RedirectToPage("./Index");
        }

        await reminderPauseStore.SetPausedUntilAsync(resumeTimestampUtc, cancellationToken);

        TempData["SuccessMessage"] = $"Påmindelserne genoptages den {resumeDate:dd-MM-yyyy}.";

        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostClearReminderPauseAsync(CancellationToken cancellationToken = default)
    {
        await reminderPauseStore.ClearPauseAsync(cancellationToken);

        TempData["SuccessMessage"] = "Pausen for påmindelser er fjernet.";

        return RedirectToPage("./Index");
    }
}
