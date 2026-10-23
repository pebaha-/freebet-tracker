using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using FreebetTracker.Web.Data;
using FreebetTracker.Web.Services;
using System.Diagnostics;
using System.Globalization;

var startupStopwatch = Stopwatch.StartNew();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.Configure<FreebetReminderOptions>(builder.Configuration.GetSection("FreebetReminder"));

builder.Services.AddScoped<FreebetReminderService>();
builder.Services.AddScoped<IEmailSender, GmailEmailSender>();

builder.Services.AddSingleton<TokenCredential>(_ =>
{
    if (builder.Environment.IsDevelopment())
    {
        return new AzureCliCredential();
    }

    return new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
});

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    var accountName = configuration["AzureStorage:AccountName"]
        ?? throw new InvalidOperationException("Azure Storage account name is not configured.");

    var endpoint = new Uri($"https://{accountName}.table.core.windows.net");

    return new TableClient(endpoint, "Freebets", sp.GetRequiredService<TokenCredential>());
});

builder.Services.AddSingleton<IFreebetRepository, FreebetTableRepository>();

var culture = new CultureInfo("da-DK");

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var app = builder.Build();

var logger = app.Logger;

logger.LogInformation("Application built in {ElapsedMs} ms. Environment: {Environment}", startupStopwatch.ElapsedMilliseconds, app.Environment.EnvironmentName);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Log samlet behandlingstid for hvert request.
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        await next();
    }
    catch (Exception exception)
    {
        stopwatch.Stop();
        logger.LogError(exception, "Request {Method} {Path} failed after {ElapsedMs} ms", context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
        throw;
    }
    finally
    {
        if (stopwatch.IsRunning)
        {
            stopwatch.Stop();
        }

        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var statusCode = context.Response.StatusCode;

        if (elapsedMs >= 1000)
        {
            logger.LogWarning("Slow request: {Method} {Path} returned {StatusCode} in {ElapsedMs} ms", context.Request.Method, context.Request.Path, statusCode, elapsedMs);
        }
        else
        {
            logger.LogInformation("Request: {Method} {Path} returned {StatusCode} in {ElapsedMs} ms", context.Request.Method, context.Request.Path, statusCode, elapsedMs);
        }
    }
});

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Lifetime.ApplicationStarted.Register(() =>
{
    logger.LogInformation("Application started. Total startup time: {ElapsedMs} ms", startupStopwatch.ElapsedMilliseconds);
});

app.Run();
