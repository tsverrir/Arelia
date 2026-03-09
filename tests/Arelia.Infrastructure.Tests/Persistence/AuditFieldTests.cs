using Arelia.Domain.Entities;
using Arelia.Infrastructure.Persistence;
using Arelia.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Infrastructure.Tests.Persistence;

public class AuditFieldTests
{
    [Fact]
    public async Task WhenAddingEntityThenCreatedAtAndCreatedByShouldBeSet()
    {
        var tenantContext = new TenantContext();
        tenantContext.CurrentUserId = "test-user-id";
        var orgId = Guid.NewGuid();
        tenantContext.SetOrganization(orgId);

        await using var context = CreateContext(tenantContext);

        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            OrganizationId = orgId
        };

        context.Persons.Add(person);
        await context.SaveChangesAsync();

        person.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        person.CreatedBy.Should().Be("test-user-id");
        person.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task WhenModifyingEntityThenUpdatedAtAndUpdatedByShouldBeSet()
    {
        var tenantContext = new TenantContext();
        tenantContext.CurrentUserId = "test-user-id";
        var orgId = Guid.NewGuid();
        tenantContext.SetOrganization(orgId);

        await using var context = CreateContext(tenantContext);

        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            OrganizationId = orgId
        };

        context.Persons.Add(person);
        await context.SaveChangesAsync();

        person.FirstName = "Updated";
        context.Entry(person).State = EntityState.Modified;
        await context.SaveChangesAsync();

        person.UpdatedAt.Should().NotBeNull();
        person.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        person.UpdatedBy.Should().Be("test-user-id");
    }

    [Fact]
    public async Task WhenNoTenantContextThenOrgIdShouldNotBeOverwritten()
    {
        await using var context = CreateContext(tenantContext: null);

        var specificOrgId = Guid.NewGuid();
        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            OrganizationId = specificOrgId
        };

        context.Persons.Add(person);
        await context.SaveChangesAsync();

        person.OrganizationId.Should().Be(specificOrgId);
    }

    private static AreliaDbContext CreateContext(TenantContext? tenantContext)
    {
        var options = new DbContextOptionsBuilder<AreliaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AreliaDbContext(options, tenantContext);
    }
}
