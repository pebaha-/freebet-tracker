using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using FreebetTracker.Web.Data;
using FreebetTracker.Web.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.Configure<FreebetReminderOptions>(builder.Configuration.GetSection("FreebetReminder"));

builder.Services.AddScoped<FreebetReminderService>();
builder.Services.AddScoped<IEmailSender, GmailEmailSender>();
builder.Services.AddScoped<IFreebetSuggestionService, FreebetSuggestionService>();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
