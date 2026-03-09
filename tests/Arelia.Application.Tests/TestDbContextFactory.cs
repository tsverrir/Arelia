using Arelia.Infrastructure.Persistence;
using Arelia.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Tests;

internal static class TestDbContextFactory
{
    public static AreliaDbContext Create(Guid? organizationId = null, string? userId = null)
    {
        var tenantContext = new TenantContext();
        if (organizationId.HasValue)
            tenantContext.SetOrganization(organizationId.Value);
        if (userId is not null)
            tenantContext.CurrentUserId = userId;

        var options = new DbContextOptionsBuilder<AreliaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AreliaDbContext(options, tenantContext);
    }
}
