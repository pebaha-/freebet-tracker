using System.ComponentModel.DataAnnotations;

namespace FreebetTracker.Web.Models;

public class FreebetInputModel
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
    [DataType(DataType.Date)]
    [Display(Name = "Udløbsdato")]
    public DateTime? ExpiryDate { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Udløbstid (valgfrit)")]
    public TimeOnly? ExpiryTime { get; set; }

    [StringLength(200)]
    [Display(Name = "Vilkår")]
    public string? Terms { get; set; }


    public Freebet ToFreebet(string? id = null, bool isUsed = false)
    {
        var expiryDate = ExpiryDate!.Value.Date;
        var expiryTimeSpecified = ExpiryTime.HasValue;

        var freebet = new Freebet
        {
            Player = Player.Trim(),
            Account = Account.Trim(),
            Amount = Amount!.Value,
            ExpiryDate = expiryTimeSpecified ? expiryDate.Add(ExpiryTime!.Value.ToTimeSpan()) : expiryDate.AddDays(1).AddTicks(-1),
            ExpiryTimeSpecified = expiryTimeSpecified,
            Terms = string.IsNullOrWhiteSpace(Terms) ? null : Terms.Trim(),
            IsUsed = isUsed
        };

        if (id is not null)
        {
            freebet.Id = id;
        }

        return freebet;
    }
}
