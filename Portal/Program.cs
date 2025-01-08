    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Identity.Web;
    using Microsoft.Identity.Web.UI;
    using Portal.DeploymentService.Class;
    using Portal.DeploymentService.Interface;

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSession();

    // Add services to the container
    builder.Services.AddRazorPages()
        .AddMicrosoftIdentityUI();

    builder.Services.AddControllers(); // Enable controllers

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


// Configure authentication
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "user.read" })
    .AddInMemoryTokenCaches();

// Configure the redirect URIs
builder.Services.Configure<MicrosoftIdentityOptions>(options =>
{
    options.Events ??= new OpenIdConnectEvents();

    options.Events.OnRedirectToIdentityProvider = context =>
    {
        // Override the redirect URI to always use localhost
        var uriBuilder = new UriBuilder(context.ProtocolMessage.RedirectUri)
        {
            Host = "localhost", // Force localhost as the host
            Port = 3000         // Ensure the port matches the Azure AD configuration
        };
        context.ProtocolMessage.RedirectUri = uriBuilder.ToString();
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToIdentityProviderForSignOut = context =>
    {
        // Override the post-logout redirect URI to always use localhost
        var uriBuilder = new UriBuilder(context.ProtocolMessage.PostLogoutRedirectUri)
        {
            Host = "localhost",
            Port = 3000
        };
        context.ProtocolMessage.PostLogoutRedirectUri = uriBuilder.ToString();
        return Task.CompletedTask;
    };
});




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

    app.UseAuthentication();
    app.UseAuthorization();

    // Map static assets and Razor Pages
    app.MapRazorPages();

    app.MapControllers(); // Map controller routes
    
    app.Run();