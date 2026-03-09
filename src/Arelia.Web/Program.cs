using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Arelia.Web.Components;
using Arelia.Web.Components.Account;
using Arelia.Web.Services;
using Arelia.Application;
using Arelia.Infrastructure;
using Arelia.Infrastructure.Identity;
using Arelia.Infrastructure.Persistence;
using Arelia.Infrastructure.Services;
using MudBlazor.Services;

namespace Arelia.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add layer services
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        // Blazor
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // MudBlazor
        builder.Services.AddMudServices();

        // Tenant service
        builder.Services.AddScoped<TenantService>();

        // Identity UI support
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityUserAccessor>();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, DevEmailSender>();

        // Localization
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

        var app = builder.Build();

        // Seed data
        await DataSeeder.SeedAsync(app.Services);

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRequestLocalization("en", "da");
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapAdditionalIdentityEndpoints();

        app.Run();
    }
}
