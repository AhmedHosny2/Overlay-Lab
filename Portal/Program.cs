using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
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

// Configure Microsoft Identity authentication
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
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.CorrelationCookie.SameSite = SameSiteMode.None;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// Middleware to handle forwarded headers (if behind a proxy)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Enable session middleware
app.UseSession();

// Enforce Content Security Policy (CSP) if needed
// app.Use(async (context, next) =>
// {
//     context.Response.Headers.Append("Content-Security-Policy", 
//         "default-src 'self'; " +
//         "script-src 'self' https://stackpath.bootstrapcdn.com https://code.jquery.com https://cdn.jsdelivr.net;");
//     await next();
// });

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

app.UseAuthentication();
app.UseAuthorization();

// Map static assets and Razor Pages
app.MapRazorPages();

app.Run();