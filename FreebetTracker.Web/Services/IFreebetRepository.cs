using FreebetTracker.Web.Models;

namespace FreebetTracker.Web.Services;

public interface IFreebetRepository
{
    Task<IReadOnlyList<Freebet>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Freebet?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task AddAsync(Freebet freebet, CancellationToken cancellationToken = default);

    Task UpdateAsync(Freebet freebet, CancellationToken cancellationToken = default);

    Task DeleteAsync(Freebet freebet, CancellationToken cancellationToken = default);
}
