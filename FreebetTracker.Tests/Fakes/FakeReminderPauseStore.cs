using FreebetTracker.Web.Services;

namespace FreebetTracker.Tests.Fakes;

public sealed class FakeReminderPauseStore : IReminderPauseStore
{
    public DateTimeOffset? PausedUntilUtc { get; private set; }

    public Task<DateTimeOffset?> GetPausedUntilAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PausedUntilUtc);
    }

    public Task SetPausedUntilAsync(DateTimeOffset pausedUntilUtc, CancellationToken cancellationToken = default)
    {
        PausedUntilUtc = pausedUntilUtc;
        return Task.CompletedTask;
    }

    public Task ClearPauseAsync(CancellationToken cancellationToken = default)
    {
        PausedUntilUtc = null;
        return Task.CompletedTask;
    }
}
