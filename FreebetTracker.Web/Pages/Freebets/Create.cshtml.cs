using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FreebetTracker.Web.Pages.Freebets;

public class CreateModel(IFreebetRepository repository, IFreebetSuggestionService suggestionService) : PageModel
{
    [BindProperty]
    public FreebetInputModel Input { get; set; } = new();
    public IReadOnlyList<string> PlayerSuggestions { get; private set; } = [];
    public IReadOnlyList<string> AccountSuggestions { get; private set; } = [];

    private async Task LoadSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var suggestions = await suggestionService.GetSuggestionsAsync(cancellationToken);

        PlayerSuggestions = suggestions.PlayerSuggestions;
        AccountSuggestions = suggestions.AccountSuggestions;
    }

    public async Task OnGetAsync()
    {
        Input.ExpiryDate = DateTime.Today;
        await LoadSuggestionsAsync();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadSuggestionsAsync(cancellationToken);
            return Page();
        }

        var freebet = Input.ToFreebet();

        await repository.AddAsync(freebet, cancellationToken);

        TempData["SuccessMessage"] = "Freebet er oprettet";

        return RedirectToPage("./Index");
    }
}
