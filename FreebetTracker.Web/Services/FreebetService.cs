using FreebetTracker.Web.Models;

namespace FreebetTracker.Web.Services;

public class FreebetService
{
    public static IReadOnlyList<Freebet> SelectForReminder(IEnumerable<Freebet> freebets, DateTime? now = null, DateTime? pausedUntil = null)
    {
        var currentTime = now ?? DateTime.Now;

        var eligible = freebets
            .Where(f => !f.IsUsed && f.ExpiryDate >= currentTime)
            .Where(f => pausedUntil is null || f.ExpiryDate <= pausedUntil.Value)
            .OrderBy(f => f.ExpiryDate)
            .ThenBy(f => f.Account)
            .ToList();

        var urgent = eligible
            .Where(f => f.ExpiryDate <= currentTime.AddDays(3))
            .ToList();

        return urgent.Count >= 3
            ? urgent
            : [.. eligible.Take(3)];
    }
}
