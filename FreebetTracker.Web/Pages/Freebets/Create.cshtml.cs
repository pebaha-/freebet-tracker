using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FreebetTracker.Web.Pages.Freebets;

public class CreateModel(IFreebetRepository repository) : PageModel
{
    [BindProperty]
    public FreebetInputModel Input { get; set; } = new();

    public void OnGet() => Input.ExpiryDate = DateTime.Today;

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var freebet = Input.ToFreebet();

        await repository.AddAsync(freebet, cancellationToken);

        TempData["SuccessMessage"] = "Freebet er oprettet";

        return RedirectToPage("./Index");
    }
}
