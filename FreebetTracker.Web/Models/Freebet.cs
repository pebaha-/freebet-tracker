namespace FreebetTracker.Web.Models;

public sealed class Freebet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Player { get; set; } = string.Empty;

    public string Account { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime ExpiryDate { get; set; }

    public bool ExpiryTimeSpecified { get; set; }

    public string? Terms { get; set; }

    public bool ReminderSent { get; set; }

    public bool IsUsed { get; set; } = false;
}
