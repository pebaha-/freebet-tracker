using FreebetTracker.Functions;
using FreebetTracker.Web.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FreebetTracker.Tests;

[TestClass]
public sealed class FreebetReminderRunnerTests
{
    [TestMethod]
    public async Task RunAsync_Should_CallReminderService()
    {
        var reminderServiceMock = new Mock<IFreebetReminderService>();
        var loggerMock = new Mock<ILogger<FreebetReminderRunner>>();
        var runner = new FreebetReminderRunner(reminderServiceMock.Object, loggerMock.Object);
        using var cancellationTokenSource = new CancellationTokenSource();

        await runner.RunAsync(cancellationTokenSource.Token);

        reminderServiceMock.Verify(service => service.SendReminderAsync(cancellationTokenSource.Token), Times.Once);
    }
}
