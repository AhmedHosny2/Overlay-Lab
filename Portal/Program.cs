using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Portal.DeploymentService.Class;
using Portal.DeploymentService.Interface;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

// Dependency injection for DeploymentService
builder.Services.AddSingleton<IDeploymentService, DeploymentService>();

// Load configuration for main appsettings
// builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Dynamically load all exercise configuration files
var exerciseConfigs = Directory.GetFiles("ExConfiguration", "*.json");
foreach (var filePath in exerciseConfigs)
{
    builder.Configuration.AddJsonFile(filePath, optional: true, reloadOnChange: true);
}

// Configure strongly-typed configuration for ExerciseConfig
builder.Services.Configure<ExerciseConfig>(options =>
{
    // Ensure correct binding if "ExerciseConfig" is a subsection
    builder.Configuration.GetSection("ExerciseConfig").Bind(options);
});

// Configure Microsoft Identity authentication
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "user.read" })
    .AddInMemoryTokenCaches();

var app = builder.Build();

// Middleware to enforce Content Security Policy (CSP)
// Uncomment and configure as needed
// app.Use(async (context, next) =>
// {
//     context.Response.Headers.Append("Content-Security-Policy", 
//         "default-src 'self'; " +
//         "script-src 'self' https://stackpath.bootstrapcdn.com https://code.jquery.com https://cdn.jsdelivr.net;");
//     await next();
// });

// Enable session middleware
app.UseSession();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map static assets and Razor Pages
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
