using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;

namespace FreebetTracker.Web.Data;

public sealed class DemoFreebetRepository : IFreebetRepository
{
    private static readonly IReadOnlyList<Freebet> Freebets =
    [
        new()
        {
            Id = "1",
            Account = "Bet365",
            Player = "Bruger A",
            Amount = 100m,
            ExpiryDate = new DateTime(2026, 10, 18),
            Terms = "Kun Superligaen",
            ReminderSent = false
        },
        new()
        {
            Id = "2",
            Account = "Unibet",
            Player = "Bruger B",
            Amount = 50m,
            ExpiryDate = new DateTime(2026, 10, 20),
            Terms = "Kun betbuilder",
            ReminderSent = false
        },
        new()
        {
            Id = "3",
            Account = "Betano",
            Player = "Bruger C",
            Amount = 75m,
            ExpiryDate = new DateTime(2026, 10, 25),
            Terms = null,
            ReminderSent = true
        }
    ];

    public Task<IReadOnlyList<Freebet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Freebets);
    }
}
