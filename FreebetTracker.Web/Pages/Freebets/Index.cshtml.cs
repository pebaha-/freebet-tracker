using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FreebetTracker.Web.Pages.Freebets;

public class IndexModel(IFreebetRepository repository) : PageModel
{
    private const int PageSize = 10;

    public IReadOnlyList<Freebet> Freebets { get; private set; } = [];

    public string StatusFilter { get; private set; } = "All";

    public string SortBy { get; private set; } = "ExpiryDate";

    public bool SortDescending { get; private set; }

    public int PageNumber { get; private set; } = 1;

    public int TotalPages { get; private set; } = 1;

    public int FilteredCount { get; private set; }

    public int TotalCount { get; private set; }

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

    public async Task OnGetAsync(string? sortBy, bool sortDescending, string? statusFilter, int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        StatusFilter = statusFilter?.ToLowerInvariant() switch
        {
            "all" => "All",
            "unused" => "Unused",
            "used" => "Used",
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

        var freebets = await repository.GetAllAsync(cancellationToken);

        TotalCount = freebets.Count();

        var filteredFreebets = StatusFilter switch
        {
            "Unused" => freebets.Where(f => !f.IsUsed),
            "Used" => freebets.Where(f => f.IsUsed),
            _ => freebets
        };

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

        FilteredCount = filteredFreebets.Count();
        TotalPages = Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));

        PageNumber = Math.Clamp(pageNumber, 1, TotalPages);

        Freebets = [..
                sortedList
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
            ];
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id, string? sortBy, bool sortDescending, string? statusFilter, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByIdAsync(id, cancellationToken);

        if (existing is null)
        {
            return NotFound();
        }

        await repository.DeleteAsync(existing, cancellationToken);

        return RedirectToPage("./Index", new { sortBy, sortDescending, statusFilter, pageNumber });
    }

    public async Task<IActionResult> OnPostMarkAsUsedAsync(string id, string? sortBy, bool sortDescending, string? statusFilter, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var freebet = await repository.GetByIdAsync(id, cancellationToken);

        if (freebet is null)
        {
            return NotFound();
        }

        freebet.IsUsed = true;

        await repository.UpdateAsync(freebet, cancellationToken);

        return RedirectToPage("./Index", new { sortBy, sortDescending, statusFilter, pageNumber });
    }
}
