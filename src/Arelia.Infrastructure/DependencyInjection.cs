using Arelia.Application.Interfaces;
using Arelia.Infrastructure.Identity;
using Arelia.Infrastructure.Persistence;
using Arelia.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            .AddEntityFrameworkStores<AreliaDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<MaintenanceState>();

        return services;
    }
}
