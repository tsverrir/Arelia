using Arelia.Infrastructure.Identity;
using Arelia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arelia.Infrastructure.Services;

public static class DataSeeder
{
    public const string AdminEmail = "admin@arelia.dev";
    public const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AreliaDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AreliaDbContext>>();

        await context.Database.MigrateAsync();

        var adminUser = await userManager.FindByEmailAsync(AdminEmail);
        if (adminUser is not null)
            return;

        adminUser = new ApplicationUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(adminUser, AdminPassword);
        if (result.Succeeded)
        {
            logger.LogInformation("Seeded admin user: {Email}", AdminEmail);
        }
        else
        {
            logger.LogError("Failed to seed admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
