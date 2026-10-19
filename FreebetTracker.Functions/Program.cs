using Azure.Data.Tables;
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

    var connectionString = configuration["AzureStorage:ConnectionString"]
        ?? throw new InvalidOperationException("Azure Storage connection string is not configured.");

    return new TableClient(connectionString, "Freebets");
});
builder.Services.AddSingleton<IFreebetRepository, FreebetTableRepository>();

builder.Build().Run();
