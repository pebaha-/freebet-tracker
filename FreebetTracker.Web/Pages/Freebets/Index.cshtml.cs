using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FreebetTracker.Web.Pages.Freebets;

public class IndexModel(IFreebetRepository repository) : PageModel
{
    public IReadOnlyList<Freebet> Freebets { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Freebets = (await repository.GetAllAsync(cancellationToken))
            .OrderBy(f => f.ExpiryDate)
            .ToList();
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

    public async Task<IActionResult> OnPostMarkAsUsedAsync(string id, CancellationToken cancellationToken)
    {
        var freebet = await repository.GetByIdAsync(id, cancellationToken);

        if (freebet is null)
        {
            return NotFound();
        }

        freebet.IsUsed = true;

        await repository.UpdateAsync(freebet, cancellationToken);

        return RedirectToPage("./Index");
    }
}
