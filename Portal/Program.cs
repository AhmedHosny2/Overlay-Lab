
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Portal.DeploymentService.Class;
using Portal.DeploymentService.Interface;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();
// dependency injection  for DeploymentService
builder.Services.AddSingleton<IDeploymentService, DeploymentService>();


// Configure Azure AD authentication
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();
// builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
//     .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Configure authorization
// builder.Services.AddAuthorization(options =>
// {
//     options.FallbackPolicy = options.DefaultPolicy;
// });

var app = builder.Build();


// Add CSP middleware to disallow inline JavaScript
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' https://stackpath.bootstrapcdn.com https://code.jquery.com https://cdn.jsdelivr.net;");
    await next();
});


// enable session
app.UseSession();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// app.UseAuthentication();

// app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
