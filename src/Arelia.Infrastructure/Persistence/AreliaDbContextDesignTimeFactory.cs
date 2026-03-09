using Arelia.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Arelia.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core tooling (migrations, scaffolding).
/// </summary>
public class AreliaDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AreliaDbContext>
{
    public AreliaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AreliaDbContext>();
        optionsBuilder.UseSqlite("Data Source=arelia.db");

        return new AreliaDbContext(optionsBuilder.Options, new TenantContext());
    }
}
