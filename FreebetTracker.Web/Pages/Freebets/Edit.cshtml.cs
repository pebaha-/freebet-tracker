using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace FreebetTracker.Web.Pages.Freebets;

public class EditModel(IFreebetRepository repository) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Spiller")]
        public string Player { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Konto")]
        public string Account { get; set; } = string.Empty;

        [Required]
        [Range(typeof(decimal), "0,01", "1000000")]
        [Display(Name = "Værdi (DKK)")]
        public decimal? Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Udløbsdato")]
        public DateTime? ExpiryDate { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "Udløbstid (valgfrit)")]
        public TimeOnly? ExpiryTime { get; set; }

        [StringLength(200)]
        [Display(Name = "Vilkår")]
        public string? Terms { get; set; }
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
            ExpiryTime = freebet.ExpiryTimeSpecified
                ? TimeOnly.FromDateTime(freebet.ExpiryDate)
                : null,
            Terms = freebet.Terms
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existing = await repository.GetByIdAsync(Input.Id, cancellationToken);

        if (existing is null)
        {
            return NotFound();
        }

        var expiryDate = Input.ExpiryDate!.Value.Date;
        var expiryTimeSpecified = Input.ExpiryTime.HasValue;

        var freebet = new Freebet
        {
            Id = existing.Id,
            Player = Input.Player.Trim(),
            Account = Input.Account.Trim(),
            Amount = Input.Amount!.Value,
            ExpiryDate = expiryTimeSpecified
                ? expiryDate.Add(Input.ExpiryTime!.Value.ToTimeSpan())
                : expiryDate.AddDays(1).AddTicks(-1),
            ExpiryTimeSpecified = expiryTimeSpecified,
            Terms = string.IsNullOrWhiteSpace(Input.Terms)
                ? null
                : Input.Terms.Trim(),
            ReminderSent = existing.ReminderSent
        };

        await repository.UpdateAsync(freebet, cancellationToken);

        return RedirectToPage("./Index");
    }
}
