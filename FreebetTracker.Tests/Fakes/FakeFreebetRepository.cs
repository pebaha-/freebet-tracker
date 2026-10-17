using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;

namespace FreebetTracker.Tests.Fakes;

internal class FakeFreebetRepository : IFreebetRepository
{
    public List<Freebet> Freebets { get; } = [];

    public Task<IReadOnlyList<Freebet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Freebet>>(Freebets);
    }

    public Task<Freebet?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Freebets.FirstOrDefault(f => f.Id == id));
    }

    public Task AddAsync(Freebet freebet, CancellationToken cancellationToken = default)
    {
        Freebets.Add(freebet);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Freebet freebet, CancellationToken cancellationToken = default)
    {
        var index = Freebets.FindIndex(f => f.Id == freebet.Id);

        if (index >= 0)
        {
            Freebets[index] = freebet;
        }

        return Task.CompletedTask;
    }
}
