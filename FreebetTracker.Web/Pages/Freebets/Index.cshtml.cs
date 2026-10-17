using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FreebetTracker.Web.Pages.Freebets;

public class IndexModel(IFreebetRepository repository) : PageModel
{
    public IReadOnlyList<Freebet> Freebets { get; private set; } = [];

    public string SortBy { get; private set; } = "ExpiryDate";

    public bool SortDescending { get; private set; }

    public bool NextSortDescending(string column)
    {
        return string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase)
            && !SortDescending;
    }

    public string SortIndicator(string column)
    {
        if (!string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return SortDescending ? " ▼" : " ▲";
    }

    public async Task OnGetAsync(string? sortBy, bool sortDescending, CancellationToken cancellationToken)
    {
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

        var sorted = SortBy switch
        {
            "Account" => SortDescending
                ? freebets.OrderByDescending(f => f.Account)
                : freebets.OrderBy(f => f.Account),

            "Player" => SortDescending
                ? freebets.OrderByDescending(f => f.Player)
                : freebets.OrderBy(f => f.Player),

            "Amount" => SortDescending
                ? freebets.OrderByDescending(f => f.Amount)
                : freebets.OrderBy(f => f.Amount),

            "ExpiryDate" => SortDescending
                ? freebets.OrderByDescending(f => f.ExpiryDate)
                : freebets.OrderBy(f => f.ExpiryDate),

            "Terms" => SortDescending
                ? freebets.OrderByDescending(f => f.Terms)
                : freebets.OrderBy(f => f.Terms),

            "IsUsed" => SortDescending
                ? freebets.OrderByDescending(f => f.IsUsed)
                : freebets.OrderBy(f => f.IsUsed),

            _ => freebets.OrderBy(f => f.ExpiryDate)
        };

        Freebets = [..sorted];
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIdAsync(id, cancellationToken);

        if (existing is null)
        {
            return NotFound();
        }

        await repository.DeleteAsync(existing, cancellationToken);

        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostMarkAsUsedAsync(string id, string sortBy, bool sortDescending, CancellationToken cancellationToken)
    {
        var freebet = await repository.GetByIdAsync(id, cancellationToken);

        if (freebet is null)
        {
            return NotFound();
        }

        freebet.IsUsed = true;

        await repository.UpdateAsync(freebet, cancellationToken);

        return RedirectToPage("./Index", new
        {
            sortBy,
            sortDescending
        });
    }
}
