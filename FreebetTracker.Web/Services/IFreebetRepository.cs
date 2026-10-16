using FreebetTracker.Web.Models;

namespace FreebetTracker.Web.Services;

public interface IFreebetRepository
{
    Task<IReadOnlyList<Freebet>> GetAllAsync(CancellationToken cancellationToken = default);
}
