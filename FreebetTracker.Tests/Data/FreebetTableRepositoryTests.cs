using Azure;
using Azure.Data.Tables;
using FreebetTracker.Web.Data;
using FreebetTracker.Web.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace FreebetTracker.Tests.Data;

[TestClass]
public class FreebetTableRepositoryTests
{
    private const string PartitionKey = "freebets";

    private Mock<TableClient> _tableClientMock = null!;
    private Mock<ILogger<FreebetTableRepository>> _loggerMock = null!;
    private FreebetTableRepository _repository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _tableClientMock = new Mock<TableClient>();
        _loggerMock = new Mock<ILogger<FreebetTableRepository>>();
        _repository = new FreebetTableRepository(_tableClientMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsAllFreebets()
    {
        var first = CreateFreebet("id-1", "Player 1", "Account 1");
        var second = CreateFreebet("id-2", "Player 2", "Account 2");

        SetupQuery(
            FreebetTableEntityMapper.MapToEntity(first),
            FreebetTableEntityMapper.MapToEntity(second));

        var result = await _repository.GetAllAsync();

        Assert.HasCount(2, result);
        CollectionAssert.AreEquivalent(new[] { "id-1", "id-2" }, result.Select(f => f.Id).ToArray());

        _tableClientMock.Verify(c => c.QueryAsync<TableEntity>(
            It.Is<string>(filter => filter == "PartitionKey eq 'freebets'"),
            It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task GetAllAsync_WhenTableIsEmpty_ReturnsEmptyList()
    {
        SetupQuery();

        var result = await _repository.GetAllAsync();

        Assert.HasCount(0, result);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenEntityExists_ReturnsFreebet()
    {
        var freebet = CreateFreebet("id-1", "Player 1", "Account 1");
        SetupGetEntity("id-1", FreebetTableEntityMapper.MapToEntity(freebet));

        var result = await _repository.GetByIdAsync("id-1");

        Assert.IsNotNull(result);
        Assert.AreEqual("id-1", result.Id);
        Assert.AreEqual("Player 1", result.Player);
        Assert.AreEqual("Account 1", result.Account);
        Assert.AreEqual(100m, result.Amount);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        _tableClientMock
            .Setup(c => c.GetEntityAsync<TableEntity>(
                PartitionKey,
                "missing",
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Not found"));

        var result = await _repository.GetByIdAsync("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task AddAsync_WhenIdIsMissing_GeneratesIdAndAddsEntity()
    {
        var freebet = CreateFreebet("", "Player 1", "Account 1");
        TableEntity? addedEntity = null;

        _tableClientMock
            .Setup(c => c.AddEntityAsync(
                It.IsAny<TableEntity>(),
                It.IsAny<CancellationToken>()))
            .Callback<TableEntity, CancellationToken>(
                (entity, _) => addedEntity = entity)
            .ReturnsAsync(Mock.Of<Response>());

        await _repository.AddAsync(freebet);

        Assert.IsFalse(string.IsNullOrWhiteSpace(freebet.Id));
        Assert.IsNotNull(addedEntity);
        Assert.AreEqual(PartitionKey, addedEntity.PartitionKey);
        Assert.AreEqual(freebet.Id, addedEntity.RowKey);

        _tableClientMock.Verify(c => c.AddEntityAsync(
            It.IsAny<TableEntity>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task AddAsync_WhenIdExists_PreservesId()
    {
        var freebet = CreateFreebet("existing-id", "Player 1", "Account 1");
        TableEntity? addedEntity = null;

        _tableClientMock
            .Setup(c => c.AddEntityAsync(
                It.IsAny<TableEntity>(),
                It.IsAny<CancellationToken>()))
            .Callback<TableEntity, CancellationToken>(
                (entity, _) => addedEntity = entity)
            .ReturnsAsync(Mock.Of<Response>());

        await _repository.AddAsync(freebet);

        Assert.IsNotNull(addedEntity);
        Assert.AreEqual("existing-id", addedEntity.RowKey);
    }

    [TestMethod]
    public async Task UpdateAsync_WhenEntityDoesNotExist_ThrowsInvalidOperationException()
    {
        _tableClientMock
            .Setup(c => c.GetEntityAsync<TableEntity>(
                PartitionKey,
                "missing",
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Not found"));

        var freebet = CreateFreebet("missing", "Player 1", "Account 1");

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _repository.UpdateAsync(freebet));

        Assert.Contains("missing", exception.Message);

        _tableClientMock.Verify(c => c.UpdateEntityAsync(
            It.IsAny<TableEntity>(),
            It.IsAny<ETag>(),
            It.IsAny<TableUpdateMode>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [TestMethod]
    public async Task DeleteAsync_DeletesEntityUsingPartitionKeyAndId()
    {
        var freebet = CreateFreebet("test-id", "Test player", "Test account");

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _tableClientMock
            .Setup(c => c.DeleteEntityAsync(
                PartitionKey,
                "test-id",
                It.IsAny<ETag>(),
                cancellationToken))
            .ReturnsAsync(Mock.Of<Response>());

        await _repository.DeleteAsync(freebet, cancellationToken);

        _tableClientMock.Verify(c => c.DeleteEntityAsync(
            PartitionKey,
            "test-id",
            It.IsAny<ETag>(),
            cancellationToken),
            Times.Once);
    }


    [TestMethod]
    public async Task GetByIdAsync_WhenIsUsedPropertyIsMissing_ReturnsUnusedFreebet()
    {
        var entity = CreateEntityWithoutIsUsed("id-1");

        SetupGetEntity("id-1", entity);

        var result = await _repository.GetByIdAsync("id-1");

        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsUsed);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenIsUsedPropertyIsNotBoolean_ReturnsUnusedFreebet()
    {
        var entity = CreateEntityWithoutIsUsed("id-1");
        entity["IsUsed"] = "true";

        SetupGetEntity("id-1", entity);

        var result = await _repository.GetByIdAsync("id-1");

        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsUsed);
    }

    private static TableEntity CreateEntityWithoutIsUsed(string id)
    {
        return new TableEntity(PartitionKey, id)
        {
            ["Player"] = "Player 1",
            ["Account"] = "Account 1",
            ["Amount"] = "100",
            ["ExpiryDate"] = new DateTime(2026, 12, 31, 18, 0, 0, DateTimeKind.Utc).ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            ["ExpiryTimeSpecified"] = true
        };
    }

    private void SetupQuery(params TableEntity[] entities)
    {
        var page = Page<TableEntity>.FromValues(entities, continuationToken: null, response: Mock.Of<Response>());

        _tableClientMock
            .Setup(c => c.QueryAsync<TableEntity>(
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<TableEntity>.FromPages([page]));
    }

    private void SetupGetEntity(string id, TableEntity entity)
    {
        _tableClientMock
            .Setup(c => c.GetEntityAsync<TableEntity>(
                PartitionKey,
                id,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
                entity,
                Mock.Of<Response>()));
    }

    private static Freebet CreateFreebet(string id, string player, string account, DateTime? expiryDate = null, bool isUsed = false, bool expiryTimeSpecified = true)
    {
        return new Freebet
        {
            Id = id,
            Player = player,
            Account = account,
            Amount = 100m,
            ExpiryDate = expiryDate ?? new DateTime(2026, 12, 31, 18, 0, 0, DateTimeKind.Local),
            ExpiryTimeSpecified = expiryTimeSpecified,
            Terms = "Test terms",
            IsUsed = isUsed
        };
    }
}
