using System.Globalization;
using Azure.Data.Tables;
using FreebetTracker.Web.Data;
using FreebetTracker.Web.Models;

namespace FreebetTracker.Tests.Data;

[TestClass]
public sealed class FreebetTableEntityMapperTests
{
    [TestMethod]
    public void MapToEntity_MapsAllProperties()
    {
        var freebet = CreateFreebet();

        var entity = FreebetTableEntityMapper.MapToEntity(freebet);

        Assert.AreEqual("freebets", entity.PartitionKey);
        Assert.AreEqual(freebet.Id, entity.RowKey);
        Assert.AreEqual(freebet.Player, entity["Player"]);
        Assert.AreEqual(freebet.Account, entity["Account"]);
        Assert.AreEqual("125.50", entity["Amount"]);
        Assert.AreEqual(freebet.ExpiryDate.ToString("O", CultureInfo.InvariantCulture), entity["ExpiryDate"]);
        Assert.IsTrue(entity["ExpiryTimeSpecified"].Equals(true));
        Assert.AreEqual(freebet.Terms, entity["Terms"]);
    }

    [TestMethod]
    public void MapToFreebet_RoundTripsFreebet()
    {
        var original = CreateFreebet();

        var entity = FreebetTableEntityMapper.MapToEntity(original);
        var result = FreebetTableEntityMapper.MapToFreebet(entity);

        Assert.AreEqual(original.Id, result.Id);
        Assert.AreEqual(original.Player, result.Player);
        Assert.AreEqual(original.Account, result.Account);
        Assert.AreEqual(original.Amount, result.Amount);
        Assert.AreEqual(original.ExpiryDate, result.ExpiryDate);
        Assert.AreEqual(original.ExpiryTimeSpecified, result.ExpiryTimeSpecified);
        Assert.AreEqual(original.Terms, result.Terms);
    }

    [TestMethod]
    public void MapToEntity_WhenTermsAreNull_OmitsTermsProperty()
    {
        var freebet = CreateFreebet();
        freebet.Terms = null;

        var entity = FreebetTableEntityMapper.MapToEntity(freebet);

        Assert.IsFalse(entity.ContainsKey("Terms"));
    }

    [TestMethod]
    public void MapToFreebet_WhenExpiryTimeIsMissing_UsesEndOfDay()
    {
        var expiryDate = new DateTime(2026, 10, 20);

        var entity = new TableEntity("freebets", "legacy-id")
        {
            ["Player"] = "Test player",
            ["Account"] = "Test account",
            ["Amount"] = "125.50",
            ["ExpiryDate"] = expiryDate.ToString("O", CultureInfo.InvariantCulture)
        };

        var result = FreebetTableEntityMapper.MapToFreebet(entity);

        Assert.IsFalse(result.ExpiryTimeSpecified);
        Assert.AreEqual(expiryDate.AddDays(1).AddTicks(-1), result.ExpiryDate);
    }

    [TestMethod]
    public void MapToFreebet_WhenTermsAreMissing_ReturnsNullTerms()
    {
        var entity = CreateMinimalEntity();

        var result = FreebetTableEntityMapper.MapToFreebet(entity);

        Assert.IsNull(result.Terms);
    }

    [TestMethod]
    public void MapToFreebet_WhenAmountUsesInvariantCulture_ParsesDecimalCorrectly()
    {
        var entity = CreateMinimalEntity();
        entity["Amount"] = "1234.56";

        var result = FreebetTableEntityMapper.MapToFreebet(entity);

        Assert.AreEqual(1234.56m, result.Amount);
    }

    [TestMethod]
    public void MapToFreebet_WhenRequiredPropertyIsMissing_Throws()
    {
        var entity = new TableEntity("freebets", "test-id")
        {
            ["Player"] = "Test player",
            ["Account"] = "Test account"
            // ExpiryDate is intentionally missing.
        };

        Assert.ThrowsExactly<InvalidOperationException>(() => FreebetTableEntityMapper.MapToFreebet(entity));
    }

    [TestMethod]
    public void MapToFreebet_WhenRequiredPropertyHasWrongType_Throws()
    {
        var entity = CreateMinimalEntity();
        entity["Player"] = 123;

        var exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => FreebetTableEntityMapper.MapToFreebet(entity));

        Assert.Contains("Player", exception.Message);
    }

    private static Freebet CreateFreebet()
    {
        return new Freebet
        {
            Id = "test-id",
            Player = "Test player",
            Account = "Test account",
            Amount = 125.50m,
            ExpiryDate = new DateTime(2026, 10, 20, 14, 35, 0),
            ExpiryTimeSpecified = true,
            Terms = "Test terms"
        };
    }

    private static TableEntity CreateMinimalEntity()
    {
        return new TableEntity("freebets", "test-id")
        {
            ["Player"] = "Test player",
            ["Account"] = "Test account",
            ["Amount"] = "100.00",
            ["ExpiryDate"] = new DateTime(2026, 10, 20).ToString("O", CultureInfo.InvariantCulture),
            ["ExpiryTimeSpecified"] = true
        };
    }
}
