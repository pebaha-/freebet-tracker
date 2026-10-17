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
            var expiryDate = DateTime.Parse(GetString(entity, "ExpiryDate"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            var expiryTimeSpecified = entity.GetBoolean("ExpiryTimeSpecified") ?? false;

            if (!expiryTimeSpecified)
            {
                expiryDate = expiryDate.Date.AddDays(1).AddTicks(-1);
            }

            freebets.Add(new Freebet
            {
                Id = entity.RowKey,
                Player = GetString(entity, "Player"),
                Account = GetString(entity, "Account"),
                Amount = decimal.Parse(GetString(entity, "Amount"), CultureInfo.InvariantCulture),
                ExpiryDate = expiryDate,
                ExpiryTimeSpecified = expiryTimeSpecified,
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

    public async Task AddAsync(Freebet freebet, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(freebet.Id))
        {
            freebet.Id = Guid.NewGuid().ToString();
        }

        var entity = new TableEntity(PartitionKey, freebet.Id)
        {
            ["Player"] = freebet.Player,
            ["Account"] = freebet.Account,
            ["Amount"] = freebet.Amount.ToString(CultureInfo.InvariantCulture),
            ["ExpiryDate"] = freebet.ExpiryDate.ToString("O", CultureInfo.InvariantCulture), ["ReminderSent"] = freebet.ReminderSent,
            ["ExpiryTimeSpecified"] = freebet.ExpiryTimeSpecified
        };

        if (freebet.Terms is not null)
        {
            entity["Terms"] = freebet.Terms;
        }

        await tableClient.AddEntityAsync(entity, cancellationToken);
    }
}
