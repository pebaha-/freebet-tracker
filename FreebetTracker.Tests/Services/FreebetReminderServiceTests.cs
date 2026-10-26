
using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FreebetTracker.Tests.Services;

[TestClass]
public sealed class FreebetReminderServiceTests
{
    private static readonly DateTime Now = DateTime.Now;

    private static Freebet CreateFreebet(string id, DateTime expiryDate, bool isUsed = false)
    {
        return new Freebet
        {
            Id = id,
            Player = "Testspiller",
            Account = id,
            Amount = 100m,
            ExpiryDate = expiryDate,
            ExpiryTimeSpecified = true,
            IsUsed = isUsed
        };
    }

    private static FreebetReminderService CreateService(IReadOnlyList<Freebet> freebets, DateTimeOffset? pausedUntil, Mock<IEmailSender> emailSender, Mock<IReminderPauseStore> pauseStore)
    {
        var repository = new Mock<IFreebetRepository>();

        repository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(freebets);

        pauseStore
            .Setup(x => x.GetPausedUntilAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pausedUntil);

        return new FreebetReminderService(
            repository.Object,
            emailSender.Object,
            Options.Create(new FreebetReminderOptions
            {
                Recipient = "test@example.com"
            }),
            NullLogger<FreebetReminderService>.Instance,
            pauseStore.Object);
    }

    [TestMethod]
    public async Task SendReminderAsync_WhenNoPauseIsActive_SendsReminder()
    {
        var emailSender = new Mock<IEmailSender>();
        var pauseStore = new Mock<IReminderPauseStore>();

        var freebets = new[]
        {
            CreateFreebet("Freebet-A", Now.AddDays(2))
        };

        var service = CreateService(freebets, null, emailSender, pauseStore);

        await service.SendReminderAsync();

        emailSender.Verify(
            x => x.SendAsync(
                "test@example.com",
                It.IsAny<string>(),
                It.Is<string>(body => body.Contains("Freebet-A")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SendReminderAsync_WhenPauseIsActive_IncludesFreebetsExpiringBeforePauseEnds()
    {
        var emailSender = new Mock<IEmailSender>();
        var pauseStore = new Mock<IReminderPauseStore>();

        var pausedUntil = DateTimeOffset.Now.AddDays(4);

        var freebets = new[]
        {
            CreateFreebet("ExpiresDuringPause", Now.AddDays(2)),
            CreateFreebet("ExpiresAfterPause", Now.AddDays(6))
        };

        var service = CreateService(freebets, pausedUntil, emailSender, pauseStore);

        await service.SendReminderAsync();

        emailSender.Verify(
            x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(body =>
                    body.Contains("ExpiresDuringPause") &&
                    !body.Contains("ExpiresAfterPause")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SendReminderAsync_WhenOnlyFreebetsExpireAfterPauseEnds_DoesNotSendEmail()
    {
        var emailSender = new Mock<IEmailSender>();
        var pauseStore = new Mock<IReminderPauseStore>();

        var pausedUntil = DateTimeOffset.Now.AddDays(2);

        var freebets = new[]
        {
            CreateFreebet("ExpiresAfterPause", Now.AddDays(5))
        };

        var service = CreateService(freebets, pausedUntil, emailSender, pauseStore);

        await service.SendReminderAsync();

        emailSender.Verify(
            x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task SendReminderAsync_WhenPauseHasExpired_ResumesReminders()
    {
        var emailSender = new Mock<IEmailSender>();
        var pauseStore = new Mock<IReminderPauseStore>();

        var pausedUntil = DateTimeOffset.Now.AddMinutes(-1);

        var freebets = new[]
        {
            CreateFreebet("FutureFreebet", Now.AddDays(5))
        };

        var service = CreateService(freebets, pausedUntil, emailSender, pauseStore);

        await service.SendReminderAsync();

        emailSender.Verify(x => x.SendAsync("test@example.com", It.IsAny<string>(), It.Is<string>(body => body.Contains("FutureFreebet")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task SendReminderAsync_WhenNoEligibleFreebetsExist_DoesNotSendEmail()
    {
        var emailSender = new Mock<IEmailSender>();
        var pauseStore = new Mock<IReminderPauseStore>();

        var freebets = new[]
        {
            CreateFreebet("UsedFreebet", Now.AddDays(1), isUsed: true)
        };

        var service = CreateService(freebets, null, emailSender, pauseStore);

        await service.SendReminderAsync();

        emailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [TestMethod]
    public async Task SendReminderAsync_IncludesFreebetWhenPauseMatchesExpirationTime()
    {
        var emailSender = new Mock<IEmailSender>();
        var pauseStore = new Mock<IReminderPauseStore>();

        var future = DateTimeOffset.Now.AddDays(6);
        var pausedUntil = future;

        var freebets = new[]
        {
            CreateFreebet("FutureFreebet", future.DateTime)
        };

        var service = CreateService(freebets, pausedUntil, emailSender, pauseStore);

        await service.SendReminderAsync();

        emailSender.Verify(
            x => x.SendAsync(
                "test@example.com",
                It.IsAny<string>(),
                It.Is<string>(body => body.Contains("FutureFreebet")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public static async Task SendReminderAsync_ExcludesFreebetWhenExpirationIsAfterPauseEnd()
    {
        var emailSender = new Mock<IEmailSender>();
        var pauseStore = new Mock<IReminderPauseStore>();

        var future = DateTimeOffset.Now.AddDays(5);
        var pausedUntil = future;

        var freebets = new[]
        {
            CreateFreebet("FutureFreebet", future.DateTime.AddTicks(1))
        };

        var service = CreateService(freebets, pausedUntil, emailSender, pauseStore);

        await service.SendReminderAsync();

        emailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
