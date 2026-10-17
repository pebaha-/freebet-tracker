using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace FreebetTracker.Web.Pages.Freebets;

public class CreateModel(IFreebetRepository repository) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
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
        [DataType(DataType.DateTime)]
        [Display(Name = "Udløbsdato")]
        public DateTime? ExpiryDate { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "Udløbstid (valgfrit)")]
        public TimeOnly? ExpiryTime { get; set; }

        [StringLength(200)]
        [Display(Name = "Vilkår")]
        public string? Terms { get; set; }
    }

    public void OnGet()
    {
        Input.ExpiryDate = DateTime.Today;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var expiryDate = Input.ExpiryDate!.Value.Date;
        var expiryTimeSpecified = Input.ExpiryTime.HasValue;

        var freebet = new Freebet
        {
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
            ReminderSent = false
        };

        await repository.AddAsync(freebet, cancellationToken);

        return RedirectToPage("./Index");
    }
}
