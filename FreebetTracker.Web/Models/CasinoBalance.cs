namespace FreebetTracker.Web.Models;

public sealed class CasinoBalance
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Provider { get; set; } = string.Empty;

    public string Player { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public DateOnly? LastChecked { get; set; }

    public DateOnly? LastWithdrawal { get; set; }

    public DateOnly? ReminderSent { get; set; }
}
