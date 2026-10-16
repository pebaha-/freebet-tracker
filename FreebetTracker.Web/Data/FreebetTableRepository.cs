using System.Globalization;
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
            freebets.Add(new Freebet
            {
                Id = entity.RowKey,
                Player = GetString(entity, "Player"),
                Account = GetString(entity, "Account"),
                Amount = decimal.Parse(GetString(entity, "Amount"), CultureInfo.InvariantCulture),
                ExpiryDate = DateTime.Parse(GetString(entity, "ExpiryDate"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                Terms = GetOptionalString(entity, "Terms"),
                ReminderSent = entity.GetBoolean("ReminderSent") ?? false
            });
        }

        return freebets;
    }

    private static string GetString(TableEntity entity, string propertyName)
    {
        return entity.TryGetValue(propertyName, out var value)
            && value is string text
                ? text
                : throw new InvalidOperationException($"Table entity is missing string property '{propertyName}'.");
    }

    private static string? GetOptionalString(TableEntity entity, string propertyName)
    {
        return entity.TryGetValue(propertyName, out var value)
            ? value as string
            : null;
    }
}
