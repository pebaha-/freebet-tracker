using Azure;
using Azure.Data.Tables;
using System.Globalization;

namespace FreebetTracker.Web.Services;

public sealed class TableReminderPauseStore(TableClient tableClient) : IReminderPauseStore
{
    private const string PartitionKey = "settings";
    private const string RowKey = "reminder-pause";
    private const string PausedUntilProperty = "PausedUntilUtc";

    public async Task<DateTimeOffset?> GetPausedUntilAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await tableClient.GetEntityAsync<TableEntity>(PartitionKey, RowKey, cancellationToken: cancellationToken);

            if (!response.Value.TryGetValue(PausedUntilProperty, out var value) || value is not string text)
            {
                return null;
            }

            return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task SetPausedUntilAsync(DateTimeOffset pausedUntilUtc, CancellationToken cancellationToken = default)
    {
        var entity = new TableEntity(PartitionKey, RowKey)
        {
            [PausedUntilProperty] = pausedUntilUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
        };

        await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task ClearPauseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await tableClient.DeleteEntityAsync(PartitionKey, RowKey, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Der er ingen aktiv indstilling at slette.
        }
    }
}