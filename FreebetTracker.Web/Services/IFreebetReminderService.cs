namespace FreebetTracker.Web.Services;

public interface IFreebetReminderService
{
    Task SendReminderAsync(CancellationToken cancellationToken = default);
}
