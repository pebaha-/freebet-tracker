using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
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
}
