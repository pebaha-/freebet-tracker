using FreebetTracker.Web.Models;

namespace FreebetTracker.Web.Services;

public class FreebetService
{
    public static IReadOnlyList<Freebet> SelectForReminder(IEnumerable<Freebet> freebets)
    {
        var eligible = freebets
            .Where(f => !f.ReminderSent && f.ExpiryDate >= DateTime.Now)
            .OrderBy(f => f.ExpiryDate)
            .ToList();

        var urgent = eligible
            .Where(f => f.ExpiryDate <= DateTime.Now.AddDays(3))
            .ToList();

        return urgent.Count >= 3
            ? urgent
            : [.. eligible.Take(3)];
    }
}
