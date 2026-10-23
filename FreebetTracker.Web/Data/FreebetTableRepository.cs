using System.Diagnostics;
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
        var totalStopwatch = Stopwatch.StartNew();

        try
        {
            if (cache.TryGetValue(CacheKey, out IReadOnlyList<Freebet>? cachedFreebets))
            {
                logger.LogInformation("GetAllAsync cache hit. Count={Count}.", cachedFreebets!.Count);
                logger.LogInformation("GetAllAsync completed in {ElapsedMilliseconds} ms.", totalStopwatch.ElapsedMilliseconds);

                return cachedFreebets;
            }

            var freebets = await cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                var queryStopwatch = Stopwatch.StartNew();
                var mappingStopwatch = new Stopwatch();
                var result = new List<Freebet>();

                try
                {
                    await foreach (var entity in tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{PartitionKey}'", cancellationToken: cancellationToken))
                    {
                        mappingStopwatch.Start();
                        result.Add(FreebetTableEntityMapper.MapToFreebet(entity));
                        mappingStopwatch.Stop();
                    }

                    queryStopwatch.Stop();

                    logger.LogInformation("Azure Table Storage query completed. Count={Count}, QueryDuration={QueryDuration} ms, MappingDuration={MappingDuration} ms.", result.Count, queryStopwatch.ElapsedMilliseconds, mappingStopwatch.ElapsedMilliseconds);

                    return (IReadOnlyList<Freebet>)result.ToArray();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Azure Table Storage query failed after {ElapsedMilliseconds} ms.", queryStopwatch.ElapsedMilliseconds);
                    throw;
                }
            });

            logger.LogInformation("GetAllAsync completed. Count={Count}, TotalDuration={TotalDuration} ms.", freebets!.Count, totalStopwatch.ElapsedMilliseconds);

            return freebets;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetAllAsync failed after {ElapsedMilliseconds} ms.", totalStopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<Freebet?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await tableClient.GetEntityAsync<TableEntity>(PartitionKey, id, cancellationToken: cancellationToken);

            logger.LogInformation("GetByIdAsync completed in {ElapsedMilliseconds} ms.", stopwatch.ElapsedMilliseconds);

            return FreebetTableEntityMapper.MapToFreebet(response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogInformation("Freebet with ID {Id} was not found. Duration={ElapsedMilliseconds} ms.", id, stopwatch.ElapsedMilliseconds);

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetByIdAsync failed for ID {Id} after {ElapsedMilliseconds} ms.", id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task AddAsync(Freebet freebet, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(freebet.Id))
            {
                freebet.Id = Guid.NewGuid().ToString();
            }

            var entity = FreebetTableEntityMapper.MapToEntity(freebet);
            await tableClient.AddEntityAsync(entity, cancellationToken);

            cache.Remove(CacheKey);

            logger.LogInformation("AddAsync completed in {ElapsedMilliseconds} ms.", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AddAsync failed after {ElapsedMilliseconds} ms.", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task UpdateAsync(Freebet freebet, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var existing = await GetByIdAsync(freebet.Id, cancellationToken);

            if (existing is null)
            {
                throw new InvalidOperationException($"Freebet with ID '{freebet.Id}' was not found.");
            }

            var entity = FreebetTableEntityMapper.MapToEntity(freebet);

            await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace, cancellationToken);

            cache.Remove(CacheKey);

            logger.LogInformation("UpdateAsync completed in {ElapsedMilliseconds} ms.", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UpdateAsync failed for ID {Id} after {ElapsedMilliseconds} ms.", freebet.Id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task DeleteAsync(Freebet freebet, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await tableClient.DeleteEntityAsync(PartitionKey, freebet.Id, cancellationToken: cancellationToken);

            cache.Remove(CacheKey);

            logger.LogInformation("DeleteAsync completed in {ElapsedMilliseconds} ms.", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeleteAsync failed for ID {Id} after {ElapsedMilliseconds} ms.", freebet.Id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
