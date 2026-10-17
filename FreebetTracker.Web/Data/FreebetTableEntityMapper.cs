using System.Globalization;
using Azure.Data.Tables;
using FreebetTracker.Web.Models;

namespace FreebetTracker.Web.Data;

internal static class FreebetTableEntityMapper
{
    private const string PartitionKey = "freebets";

    public static TableEntity MapToEntity(Freebet freebet)
    {
        var entity = new TableEntity(PartitionKey, freebet.Id)
        {
            ["Player"] = freebet.Player,
            ["Account"] = freebet.Account,
            ["Amount"] = freebet.Amount.ToString(CultureInfo.InvariantCulture),
            ["ExpiryDate"] = freebet.ExpiryDate.ToString("O", CultureInfo.InvariantCulture),
            ["ExpiryTimeSpecified"] = freebet.ExpiryTimeSpecified,
            ["ReminderSent"] = freebet.ReminderSent,
            ["IsUsed"] = freebet.IsUsed
        };

        if (freebet.Terms is not null)
        {
            entity["Terms"] = freebet.Terms;
        }

        return entity;
    }

    public static Freebet MapToFreebet(TableEntity entity)
    {
        var expiryDate = DateTime.Parse(GetString(entity, "ExpiryDate"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        var expiryTimeSpecified = entity.GetBoolean("ExpiryTimeSpecified") ?? false;

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
            ReminderSent = entity.GetBoolean("ReminderSent") ?? false,
            IsUsed = entity.TryGetValue("IsUsed", out var isUsed)
                && isUsed is bool used
                && used
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
