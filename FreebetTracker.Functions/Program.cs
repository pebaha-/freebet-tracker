using Azure.Data.Tables;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using FreebetTracker.Functions;
using FreebetTracker.Web.Data;
using FreebetTracker.Web.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Services.Configure<FreebetReminderOptions>(builder.Configuration.GetSection("FreebetReminder"));
builder.Services.AddScoped<FreebetReminderRunner>();
builder.Services.AddScoped<IFreebetReminderService>(sp => ActivatorUtilities.CreateInstance<FreebetReminderService>(sp));
builder.Services.AddScoped<IEmailSender, GmailEmailSender>();
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
    var accountName = configuration["AzureStorage:AccountName"]
        ?? throw new InvalidOperationException("Azure Storage account name is not configured.");

    var endpoint = new Uri($"https://{accountName}.table.core.windows.net");

    var environment = sp.GetRequiredService<IHostEnvironment>();

    Azure.Core.TokenCredential credential = environment.IsDevelopment()
        ? new AzureCliCredential()
        : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);

    return new TableClient(endpoint, "Freebets", credential);
});
builder.Services.AddSingleton<IFreebetRepository, FreebetTableRepository>();
builder.Services.AddSingleton<IReminderPauseStore, TableReminderPauseStore>();

builder.Build().Run();
