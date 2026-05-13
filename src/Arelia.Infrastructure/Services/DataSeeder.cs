using Arelia.Infrastructure.Identity;
using Arelia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arelia.Infrastructure.Services;

public static class DataSeeder
{
    public const string SystemAdminRole = "SystemAdmin";
    public const string DefaultAdminEmail = "admin@arelia.dev";
    public const string DefaultAdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AreliaDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AreliaDbContext>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var adminEmail = configuration["ARELIA_ADMIN_EMAIL"]
            ?? configuration["Seed:AdminEmail"]
            ?? DefaultAdminEmail;
        var adminPassword = configuration["ARELIA_ADMIN_PASSWORD"]
            ?? configuration["Seed:AdminPassword"]
            ?? DefaultAdminPassword;

        await context.Database.MigrateAsync();

        // Ensure SystemAdmin role exists
        if (!await roleManager.RoleExistsAsync(SystemAdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(SystemAdminRole));
            logger.LogInformation("Created Identity role: {Role}", SystemAdminRole);
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is not null)
        {
            // Ensure existing seed user has the SystemAdmin role
            if (!await userManager.IsInRoleAsync(adminUser, SystemAdminRole))
                await userManager.AddToRoleAsync(adminUser, SystemAdminRole);
            return;
        }

        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, SystemAdminRole);
            logger.LogInformation("Seeded system admin user: {Email}", adminEmail);
        }
        else
        {
            logger.LogError("Failed to seed admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
