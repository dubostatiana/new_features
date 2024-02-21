using AxorP1;
using AxorP1.Components;
using AxorP1.Services;
using Blazored.LocalStorage;
using Syncfusion.Blazor;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSyncfusionBlazor();
builder.Services.AddRazorPages();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = true;
});

// Syncfusion localization service to localize Syncfusion Blazor components
builder.Services.AddSingleton(typeof(ISyncfusionStringLocalizer), typeof(SyncfusionLocalizer));

// Custom services
builder.Services.AddScoped<DataProvider>();
builder.Services.AddScoped<ThemeProvider>();

// ILogger configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddEventLog();

var app = builder.Build();
//Register Syncfusion license https://help.syncfusion.com/common/essential-studio/licensing/how-to-generate
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NAaF1cWWhLYVJwWmFZfVpgfV9HZFZUR2YuP1ZhSXxXdkdiWn5bcXxXQWZaUEU=");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRequestLocalization("fr-FR");

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
