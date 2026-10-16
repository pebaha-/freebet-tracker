using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;

namespace FreebetTracker.Tests;

[TestClass]
public sealed class FreebetSelectionTests
{
    [TestMethod]
    public void Should_Select_All_Freebets_When_There_Are_Only_Two_Eligible()
    {
        // Arrange
        var freebets = new List<Freebet>
        {
            new() { ExpiryDate = DateTime.Now.AddDays(1), ReminderSent = false },
            new() { ExpiryDate = DateTime.Now.AddDays(2), ReminderSent = false }
        };
        // Act
        var selectedFreebets = FreebetService.SelectForReminder(freebets);
        // Assert
        Assert.HasCount(2, selectedFreebets);
    }

    [TestMethod]
    public void Should_Select_Three_Freebets_When_There_Are_More_Than_Three_Eligible()
    {
        // Arrange
        var freebets = new List<Freebet>
        {
            new() { ExpiryDate = DateTime.Now.AddDays(1), ReminderSent = false },
            new() { ExpiryDate = DateTime.Now.AddDays(2), ReminderSent = false },
            new() { ExpiryDate = DateTime.Now.AddDays(3), ReminderSent = false },
            new() { ExpiryDate = DateTime.Now.AddDays(4), ReminderSent = false }
        };
        // Act
        var selectedFreebets = FreebetService.SelectForReminder(freebets);
        // Assert
        Assert.HasCount(3, selectedFreebets);
    }

    [TestMethod]
    public void Should_Select_Future_Freebet_When_There_Are_Not_Enough_Urgent()
    {
        // Arrange
        var freebets = new List<Freebet>
        {
            new() { ExpiryDate = DateTime.Now.AddDays(1), ReminderSent = false },
            new() { ExpiryDate = DateTime.Now.AddDays(14), ReminderSent = false }
        };
        // Act
        var selectedFreebets = FreebetService.SelectForReminder(freebets);
        // Assert
        Assert.HasCount(2, selectedFreebets);
    }
}
