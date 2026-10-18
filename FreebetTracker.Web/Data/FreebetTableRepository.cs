using Azure;
using Azure.Data.Tables;
using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;

namespace FreebetTracker.Web.Data;

public sealed class FreebetTableRepository(TableClient tableClient) : IFreebetRepository
{
    private const string PartitionKey = "freebets";

    public async Task<IReadOnlyList<Freebet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var freebets = new List<Freebet>();

        await foreach (var entity in tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{PartitionKey}'", cancellationToken: cancellationToken))
        {
            freebets.Add(FreebetTableEntityMapper.MapToFreebet(entity));
        }

        return freebets;
    }

    public async Task<Freebet?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await tableClient.GetEntityAsync<TableEntity>(PartitionKey, id, cancellationToken: cancellationToken);
            return FreebetTableEntityMapper.MapToFreebet(response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task AddAsync(Freebet freebet, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(freebet.Id))
        {
            freebet.Id = Guid.NewGuid().ToString();
        }

        var entity = FreebetTableEntityMapper.MapToEntity(freebet);
        await tableClient.AddEntityAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(Freebet freebet, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(freebet.Id, cancellationToken);

        if (existing is null)
        {
            throw new InvalidOperationException($"Freebet with ID '{freebet.Id}' was not found.");
        }

        var expiryChanged =
            existing.ExpiryDate != freebet.ExpiryDate ||
            existing.ExpiryTimeSpecified != freebet.ExpiryTimeSpecified;

        var entity = FreebetTableEntityMapper.MapToEntity(freebet);

        await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task DeleteAsync(Freebet freebet, CancellationToken cancellationToken = default)
    {
        await tableClient.DeleteEntityAsync(PartitionKey, freebet.Id, cancellationToken: cancellationToken);
    }
}
