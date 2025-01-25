using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Portal.DeploymentService.Class;
using Portal.DeploymentService.Interface;
using Microsoft.Extensions.Logging;
using Docker.DotNet;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();


// Create a temporary logger for configuration purposes
using var tempLoggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
});
var tempLogger = tempLoggerFactory.CreateLogger<Program>();

// Add services to the container
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

builder.Services.AddControllers();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Register DockerClient as a singleton
builder.Services.AddSingleton<DockerClient>(sp =>

{
    return new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
        .CreateClient();
});

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
// builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
//     .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "user.read" })
//     .AddInMemoryTokenCaches();

// Configure Cookie Policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
});

// Configure OpenID Connect Options
// builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
// {
//     options.CorrelationCookie.SameSite = SameSiteMode.None;
//     options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
// });

// **Move the MicrosoftIdentityOptions configuration before building the app**
// builder.Services.Configure<MicrosoftIdentityOptions>(options =>
// {
//     options.Events ??= new OpenIdConnectEvents();

//     options.Events.OnRedirectToIdentityProvider = context =>
//     {
//         // Log Redirect URI
//         tempLogger.LogInformation($"Redirecting to Identity Provider with URI: {context.ProtocolMessage.RedirectUri}");

//         var uriBuilder = new UriBuilder(context.ProtocolMessage.RedirectUri)
//         {
//             Host = "localhost",
//             Port = 3000
//         };
//         context.ProtocolMessage.RedirectUri = uriBuilder.ToString();
//         return Task.CompletedTask;
//     };

//     options.Events.OnRedirectToIdentityProviderForSignOut = context =>
//     {
//         // Log Post-Logout Redirect URI
//         tempLogger.LogInformation($"Post-logout redirect URI: {context.ProtocolMessage.PostLogoutRedirectUri}");

//         var uriBuilder = new UriBuilder(context.ProtocolMessage.PostLogoutRedirectUri)
//         {
//             Host = "localhost",
//             Port = 3000
//         };
//         context.ProtocolMessage.PostLogoutRedirectUri = uriBuilder.ToString();
//         return Task.CompletedTask;
//     };
// });


// Build the application
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseSession();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1,
    KnownNetworks = { },
    KnownProxies = { }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCookiePolicy();

// app.UseAuthentication();
// app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
