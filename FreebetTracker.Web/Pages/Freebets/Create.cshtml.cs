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
        [Display(Name = "Player")]
        public string Player { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Account")]
        public string Account { get; set; } = string.Empty;

        [Required]
        [Range(typeof(decimal), "0,01", "1000000")]
        [Display(Name = "Amount (DKK)")]
        public decimal? Amount { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Expiry date")]
        public DateTime? ExpiryDate { get; set; }

        [StringLength(200)]
        [Display(Name = "Terms")]
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

        var freebet = new Freebet
        {
            Player = Input.Player.Trim(),
            Account = Input.Account.Trim(),
            Amount = Input.Amount!.Value,
            ExpiryDate = Input.ExpiryDate!.Value.Date,
            Terms = string.IsNullOrWhiteSpace(Input.Terms)
                ? null
                : Input.Terms.Trim(),
            ReminderSent = false
        };

        await repository.AddAsync(freebet, cancellationToken);

        return RedirectToPage("./Index");
    }
}
