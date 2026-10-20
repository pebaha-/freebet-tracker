using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FreebetTracker.Tests.Services;

[TestClass]
public sealed class FreebetReminderServiceTests
{
    private const string Recipient = "recipient@gmail.com";

    private Mock<IFreebetRepository> _repositoryMock = null!;
    private Mock<IEmailSender> _emailSenderMock = null!;
    private Mock<ILogger<FreebetReminderService>> _loggerMock = null!;
    private FreebetReminderService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IFreebetRepository>();
        _emailSenderMock = new Mock<IEmailSender>();
        _loggerMock = new Mock<ILogger<FreebetReminderService>>();

        _service = new FreebetReminderService(
            _repositoryMock.Object,
            _emailSenderMock.Object,
            Options.Create(new FreebetReminderOptions
            {
                Recipient = Recipient
            }),
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task SendReminderAsync_Should_SendEmailWithSelectedFreebetDetails()
    {
        // Arrange
        var freebet = new Freebet
        {
            Id = "freebet-1",
            Player = "Peter",
            Account = "Bet365",
            Amount = 100m,
            ExpiryDate = DateTime.Now.AddDays(1),
            ExpiryTimeSpecified = true,
            Terms = "Minimum odds 2.00",
            IsUsed = false
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Freebet>)[freebet]);

        // Act
        await _service.SendReminderAsync();

        // Assert
        _emailSenderMock.Verify(
            e => e.SendAsync(
                Recipient,
                "Freebet-påmindelser",
                It.Is<string>(body =>
                    body.Contains("Bet365") &&
                    body.Contains("Peter") &&
                    body.Contains("100,00 kr.") &&
                    body.Contains("Minimum odds 2.00")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SendReminderAsync_Should_NotSendEmail_WhenNoFreebetsExist()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Freebet>)[]);

        // Act
        await _service.SendReminderAsync();

        // Assert
        _emailSenderMock.Verify(
            e => e.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task SendReminderAsync_Should_NotSendEmail_WhenAllFreebetsAreUsedOrExpired()
    {
        // Arrange
        var freebets = new List<Freebet>
        {
            new()
            {
                Id = "used",
                Account = "Bet365",
                ExpiryDate = DateTime.Now.AddDays(1),
                IsUsed = true
            },
            new()
            {
                Id = "expired",
                Account = "Unibet",
                ExpiryDate = DateTime.Now.AddDays(-1),
                IsUsed = false
            }
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Freebet>)freebets);

        // Act
        await _service.SendReminderAsync();

        // Assert
        _emailSenderMock.Verify(
            e => e.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
