using Azure;
using Azure.Data.Tables;
using FreebetTracker.Web.Models;
using FreebetTracker.Web.Services;
using Microsoft.Extensions.Caching.Memory;

namespace FreebetTracker.Web.Data;

public sealed class FreebetTableRepository(TableClient tableClient, ILogger<FreebetTableRepository> logger, IMemoryCache cache) : IFreebetRepository
{
    private const string PartitionKey = "freebets";
    private const string CacheKey = "FreebetTableRepository.All";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<Freebet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cachedFreebets = await cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var freebets = new List<Freebet>();

                try
                {
                    await foreach (var entity in tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{PartitionKey}'", cancellationToken: cancellationToken))
                    {
                        logger.LogDebug("Received a table entity.");
                        freebets.Add(FreebetTableEntityMapper.MapToFreebet(entity));
                    }

                    logger.LogInformation("Azure Table Storage query completed. Count={Count}.", freebets.Count);

                    return (IReadOnlyList<Freebet>)freebets.ToArray();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Azure Table Storage query failed.");
                    throw;
                }
            });

        return cachedFreebets!;
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

        cache.Remove(CacheKey);
    }

    public async Task UpdateAsync(Freebet freebet, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(freebet.Id, cancellationToken);

        if (existing is null)
        {
            throw new InvalidOperationException($"Freebet with ID '{freebet.Id}' was not found.");
        }

        var entity = FreebetTableEntityMapper.MapToEntity(freebet);

        await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace, cancellationToken);

        cache.Remove(CacheKey);
    }

    public async Task DeleteAsync(Freebet freebet, CancellationToken cancellationToken = default)
    {
        await tableClient.DeleteEntityAsync(PartitionKey, freebet.Id, cancellationToken: cancellationToken);

        cache.Remove(CacheKey);
    }
}
