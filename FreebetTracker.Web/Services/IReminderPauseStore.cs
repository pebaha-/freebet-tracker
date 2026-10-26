namespace FreebetTracker.Web.Services;

public interface IReminderPauseStore
{
    Task<DateTimeOffset?> GetPausedUntilAsync(CancellationToken cancellationToken = default);

    Task SetPausedUntilAsync(DateTimeOffset pausedUntilUtc, CancellationToken cancellationToken = default);

    Task ClearPauseAsync(CancellationToken cancellationToken = default);
}
