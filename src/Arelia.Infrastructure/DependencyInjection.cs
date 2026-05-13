using Arelia.Application.Interfaces;
using Arelia.Infrastructure.Identity;
using Arelia.Infrastructure.Persistence;
using Arelia.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resend;

namespace Arelia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=arelia.db";

        services.AddDbContext<AreliaDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IAreliaDbContext>(provider => provider.GetRequiredService<AreliaDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AreliaDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            // Replace the default DataProtector token provider with URL-safe Base64url variant
            .AddTokenProvider<InvitationTokenProvider>(TokenOptions.DefaultProvider);

        // Invitation links are valid for 7 days
        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromDays(7));

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFileStorageService, DiskFileStorageService>();
        services.AddScoped<IHtmlSanitizerService, HtmlSanitizerService>();
        services.AddScoped<IPdfExportService, QuestPdfExportService>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<MaintenanceState>();

        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = configuration["Resend:ApiToken"] ?? string.Empty;
        });
        services.AddTransient<IResend, ResendClient>();

        var resendApiToken = configuration["Resend:ApiToken"];
        if (!string.IsNullOrWhiteSpace(resendApiToken))
        {
            services.AddTransient<IEmailSender<ApplicationUser>, ResendEmailSender>();
            services.AddTransient<IAreliaEmailService, ResendEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender<ApplicationUser>, DevEmailSender>();
            services.AddSingleton<IAreliaEmailService, DevEmailSender>();
        }

        return services;
    }
}
