using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Portal.DeploymentService.Class;
using Portal.DeploymentService.Interface;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Optionally, add other logging providers if needed
// builder.Logging.AddDebug();

// Create a temporary logger for configuration purposes
using var tempLoggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    // Add other logging providers if necessary
});
var tempLogger = tempLoggerFactory.CreateLogger<Program>();

// Add services to the container
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

builder.Services.AddControllers(); // Enable controllers

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Dependency injection for DeploymentService
builder.Services.AddSingleton<IDeploymentService, DeploymentService>();

// Dynamically load all exercise configuration files
var exerciseConfigs = Directory.GetFiles("ExConfiguration", "*.json");
foreach (var filePath in exerciseConfigs)
{
    builder.Configuration.AddJsonFile(filePath, optional: true, reloadOnChange: true);
}

// Configure strongly-typed configuration for ExerciseConfig
builder.Services.Configure<ExerciseConfig>(options =>
{
    builder.Configuration.GetSection("ExerciseConfig").Bind(options);
});

// Configure authentication
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "user.read" })
    .AddInMemoryTokenCaches();

// Configure Cookie Policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
});

// Configure OpenID Connect Options
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.CorrelationCookie.SameSite = SameSiteMode.None;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
});

// **Move the MicrosoftIdentityOptions configuration before building the app**
builder.Services.Configure<MicrosoftIdentityOptions>(options =>
{
    options.Events ??= new OpenIdConnectEvents();

    options.Events.OnRedirectToIdentityProvider = context =>
    {
        // Log Redirect URI
        tempLogger.LogInformation($"Redirecting to Identity Provider with URI: {context.ProtocolMessage.RedirectUri}");

        var uriBuilder = new UriBuilder(context.ProtocolMessage.RedirectUri)
        {
            Host = "localhost",
            Port = 3000
        };
        context.ProtocolMessage.RedirectUri = uriBuilder.ToString();
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToIdentityProviderForSignOut = context =>
    {
        // Log Post-Logout Redirect URI
        tempLogger.LogInformation($"Post-logout redirect URI: {context.ProtocolMessage.PostLogoutRedirectUri}");

        var uriBuilder = new UriBuilder(context.ProtocolMessage.PostLogoutRedirectUri)
        {
            Host = "localhost",
            Port = 3000
        };
        context.ProtocolMessage.PostLogoutRedirectUri = uriBuilder.ToString();
        return Task.CompletedTask;
    };
});
// Build the application
var app = builder.Build();

// Obtain a logger instance from the DI container
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Middleware to handle forwarded headers (if behind a proxy)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Enable session middleware
app.UseSession();

// Reconfigure forwarded headers if necessary
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1, // Optional: Limits the number of forwarders to trust
    KnownNetworks = { }, // Optional: Add known proxy IPs here if required
    KnownProxies = { } // Optional: Add specific proxy IPs here
});

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Apply Cookie Policy before Authentication
app.UseCookiePolicy();

// app.UseAuthentication();
// app.UseAuthorization();

// Log configuration details after the app is built
logger.LogInformation("Azure AD Configuration:");
logger.LogInformation($"Instance: {builder.Configuration["AzureAd:Instance"]}");
logger.LogInformation($"Domain: {builder.Configuration["AzureAd:Domain"]}");
logger.LogInformation($"TenantId: {builder.Configuration["AzureAd:TenantId"]}");
logger.LogInformation($"ClientId: {builder.Configuration["AzureAd:ClientId"]}");
logger.LogInformation($"CallbackPath: {builder.Configuration["AzureAd:CallbackPath"]}");

// Map static assets and Razor Pages
app.MapRazorPages();
app.MapControllers(); // Map controller routes

// Start the application
app.Run();
