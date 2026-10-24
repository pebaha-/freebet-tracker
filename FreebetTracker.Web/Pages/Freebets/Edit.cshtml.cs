using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace FreebetTracker.Web.Pages.Freebets;

public class EditModel(IFreebetRepository repository, IFreebetSuggestionService suggestionService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();
    public IReadOnlyList<string> PlayerSuggestions { get; private set; } = [];
    public IReadOnlyList<string> AccountSuggestions { get; private set; } = [];

    private async Task LoadSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var suggestions = await suggestionService.GetSuggestionsAsync(cancellationToken);

        PlayerSuggestions = suggestions.PlayerSuggestions;
        AccountSuggestions = suggestions.AccountSuggestions;
    }


    public sealed class InputModel : FreebetInputModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Brugt")]
        public bool IsUsed { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        var freebet = await repository.GetByIdAsync(id, cancellationToken);

        if (freebet is null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Id = freebet.Id,
            Player = freebet.Player,
            Account = freebet.Account,
            Amount = freebet.Amount,
            ExpiryDate = freebet.ExpiryDate.Date,
            ExpiryTime = freebet.ExpiryTimeSpecified ? TimeOnly.FromDateTime(freebet.ExpiryDate) : null,
            Terms = freebet.Terms,
            IsUsed = freebet.IsUsed
        };

        await LoadSuggestionsAsync(cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadSuggestionsAsync(cancellationToken);
            return Page();
        }

        var existing = await repository.GetByIdAsync(Input.Id, cancellationToken);

        if (existing is null)
        {
            return NotFound();
        }

        var freebet = Input.ToFreebet(existing.Id, Input.IsUsed);

        await repository.UpdateAsync(freebet, cancellationToken);

        TempData["SuccessMessage"] = "Ændringerne er gemt";

        return RedirectToPage("./Index");
    }
}
