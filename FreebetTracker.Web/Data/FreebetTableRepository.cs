using System.Globalization;
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
            freebets.Add(MapToFreebet(entity));
        }

        return freebets;
    }

    public async Task<Freebet?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await tableClient.GetEntityAsync<TableEntity>(PartitionKey, id, cancellationToken: cancellationToken);
            return MapToFreebet(response.Value);
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

        var entity = MapToEntity(freebet);
        await tableClient.AddEntityAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(Freebet freebet,  CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(freebet.Id, cancellationToken);

        if (existing is null)
        {
            throw new InvalidOperationException($"Freebet with ID '{freebet.Id}' was not found.");
        }

        var expiryChanged =
            existing.ExpiryDate != freebet.ExpiryDate ||
            existing.ExpiryTimeSpecified != freebet.ExpiryTimeSpecified;

        // Preserve the reminder status unless the expiry changes.
        freebet.ReminderSent = expiryChanged
            ? false
            : existing.ReminderSent;

        var entity = MapToEntity(freebet);

        await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace, cancellationToken);
    }

    private static TableEntity MapToEntity(Freebet freebet)
    {
        var entity = new TableEntity(PartitionKey, freebet.Id)
        {
            ["Player"] = freebet.Player,
            ["Account"] = freebet.Account,
            ["Amount"] = freebet.Amount.ToString(CultureInfo.InvariantCulture),
            ["ExpiryDate"] = freebet.ExpiryDate.ToString("O", CultureInfo.InvariantCulture),
            ["ExpiryTimeSpecified"] = freebet.ExpiryTimeSpecified,
            ["ReminderSent"] = freebet.ReminderSent
        };

        if (freebet.Terms is not null)
        {
            entity["Terms"] = freebet.Terms;
        }

        return entity;
    }

    private static Freebet MapToFreebet(TableEntity entity)
    {
        var expiryDate = DateTime.Parse(GetString(entity, "ExpiryDate"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        var expiryTimeSpecified = entity.GetBoolean("ExpiryTimeSpecified") ?? false;

        // Records without an explicit time are assumed to expire at day's end.
        if (!expiryTimeSpecified)
        {
            expiryDate = expiryDate.Date.AddDays(1).AddTicks(-1);
        }

        return new Freebet
        {
            Id = entity.RowKey,
            Player = GetString(entity, "Player"),
            Account = GetString(entity, "Account"),
            Amount = decimal.Parse(GetString(entity, "Amount"), CultureInfo.InvariantCulture),
            ExpiryDate = expiryDate,
            ExpiryTimeSpecified = expiryTimeSpecified,
            Terms = GetOptionalString(entity, "Terms"),
            ReminderSent = entity.GetBoolean("ReminderSent") ?? false
        };
    }

    private static string GetString(TableEntity entity, string propertyName)
    {
        return entity.TryGetValue(propertyName, out var value)
            && value is string text
                ? text
                : throw new InvalidOperationException(
                    $"Table entity is missing string property '{propertyName}'.");
    }

    private static string? GetOptionalString(TableEntity entity, string propertyName)
    {
        return entity.TryGetValue(propertyName, out var value)
            ? value as string
            : null;
    }
}
