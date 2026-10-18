using Azure.Data.Tables;
using FreebetTracker.Web.Data;
using FreebetTracker.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.Configure<FreebetReminderOptions>(builder.Configuration.GetSection("FreebetReminder"));
builder.Services.AddScoped<FreebetReminderService>();
builder.Services.AddScoped<IEmailSender, GmailEmailSender>();
builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration[
        "AzureStorage:ConnectionString"]
        ?? throw new InvalidOperationException("Azure Storage connection string is not configured.");

    return new TableClient(connectionString, "Freebets");
});

builder.Services.AddSingleton<IFreebetRepository, FreebetTableRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
