using Azure;
using Azure.Data.Tables;
using FreebetTracker.Web.Data;
using FreebetTracker.Web.Models;
using Moq;

namespace FreebetTracker.Tests.Data;

[TestClass]
public class FreebetTableRepositoryTests
{
    private const string PartitionKey = "freebets";

    private Mock<TableClient> _tableClientMock = null!;
    private FreebetTableRepository _repository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _tableClientMock = new Mock<TableClient>();
        _repository = new FreebetTableRepository(_tableClientMock.Object);
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
    public async Task UpdateAsync_WhenExpiryIsUnchanged_PreservesReminderSent()
    {
        var existing = CreateFreebet("id-1", "Player 1", "Account 1", reminderSent: true);
        SetupGetEntity("id-1", FreebetTableEntityMapper.MapToEntity(existing));
        var updated = CreateFreebet("id-1", "Player 1", "Account 1", reminderSent: false);
        TableEntity? updatedEntity = null;
        SetupUpdateCapture(entity => updatedEntity = entity);

        await _repository.UpdateAsync(updated);

        Assert.IsNotNull(updatedEntity);
        Assert.IsTrue((bool)updatedEntity["ReminderSent"]);
        Assert.IsTrue(updated.ReminderSent);

        VerifyReplaceUpdate();
    }

    [TestMethod]
    public async Task UpdateAsync_WhenExpiryDateChanges_ResetsReminderSent()
    {
        var existing = CreateFreebet("id-1", "Player 1", "Account 1", reminderSent: true);
        SetupGetEntity("id-1", FreebetTableEntityMapper.MapToEntity(existing));
        var updated = CreateFreebet("id-1", "Player 1", "Account 1", expiryDate: existing.ExpiryDate.AddDays(1), reminderSent: true);
        TableEntity? updatedEntity = null;
        SetupUpdateCapture(entity => updatedEntity = entity);

        await _repository.UpdateAsync(updated);

        Assert.IsNotNull(updatedEntity);
        Assert.IsFalse((bool)updatedEntity["ReminderSent"]);
        Assert.IsFalse(updated.ReminderSent);
    }

    [TestMethod]
    public async Task UpdateAsync_WhenExpiryTimeSpecifiedChanges_ResetsReminderSent()
    {
        var existing = CreateFreebet("id-1", "Player 1", "Account 1", reminderSent: true, expiryTimeSpecified: false);
        SetupGetEntity("id-1", FreebetTableEntityMapper.MapToEntity(existing));
        var updated = CreateFreebet("id-1", "Player 1", "Account 1", expiryDate: existing.ExpiryDate, reminderSent: true, expiryTimeSpecified: true);
        TableEntity? updatedEntity = null;
        SetupUpdateCapture(entity => updatedEntity = entity);

        await _repository.UpdateAsync(updated);

        Assert.IsNotNull(updatedEntity);
        Assert.IsFalse((bool)updatedEntity["ReminderSent"]);
        Assert.IsFalse(updated.ReminderSent);
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

    private void SetupUpdateCapture(Action<TableEntity> capture)
    {
        _tableClientMock
            .Setup(c => c.UpdateEntityAsync(
                It.IsAny<TableEntity>(),
                It.IsAny<ETag>(),
                It.IsAny<TableUpdateMode>(),
                It.IsAny<CancellationToken>()))
            .Callback<TableEntity, ETag, TableUpdateMode, CancellationToken>(
                (entity, _, _, _) => capture(entity))
            .ReturnsAsync(Mock.Of<Response>());
    }

    private void VerifyReplaceUpdate()
    {
        _tableClientMock.Verify(c => c.UpdateEntityAsync(
            It.IsAny<TableEntity>(),
            ETag.All,
            TableUpdateMode.Replace,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Freebet CreateFreebet(string id, string player, string account, DateTime? expiryDate = null, bool reminderSent = false, bool expiryTimeSpecified = true)
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
            ReminderSent = reminderSent
        };
    }
}
