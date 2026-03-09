using Arelia.Application.Organizations.Commands;
using Arelia.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Tests.Organizations;

public class CreateOrganizationTests
{
    [Fact]
    public async Task WhenCreatingOrganizationThenOrgIsCreated()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrganizationHandler(context);

        var orgId = await handler.Handle(
            new CreateOrganizationCommand("Test Choir", "test@choir.dk", "+45 1234", "user-1"),
            CancellationToken.None);

        var org = await context.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == orgId);
        org.Should().NotBeNull();
        org!.Name.Should().Be("Test Choir");
        org.ContactEmail.Should().Be("test@choir.dk");
        org.DefaultCurrencyCode.Should().Be("DKK");
    }

    [Fact]
    public async Task WhenCreatingOrganizationThenSystemRolesAreSeeded()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrganizationHandler(context);

        var orgId = await handler.Handle(
            new CreateOrganizationCommand("Test Choir", null, null, "user-1"),
            CancellationToken.None);

        var roles = await context.Roles.IgnoreQueryFilters()
            .Where(r => r.OrganizationId == orgId)
            .ToListAsync();

        roles.Should().HaveCount(4);
        roles.Select(r => r.Name).Should().Contain(new[] { "Board", "Treasurer", "Conductor", "Admin" });
        roles.Should().OnlyContain(r => r.RoleType == RoleType.System);
    }

    [Fact]
    public async Task WhenCreatingOrganizationThenAdminRoleHasAllPermissions()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrganizationHandler(context);

        var orgId = await handler.Handle(
            new CreateOrganizationCommand("Test Choir", null, null, "user-1"),
            CancellationToken.None);

        var adminRole = await context.Roles.IgnoreQueryFilters()
            .FirstAsync(r => r.OrganizationId == orgId && r.Name == "Admin");

        var permissions = await context.RolePermissions.IgnoreQueryFilters()
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.Permission)
            .ToListAsync();

        permissions.Should().HaveCount(Enum.GetValues<Permission>().Length);
    }

    [Fact]
    public async Task WhenCreatingOrganizationThenCreatorBecomesAdmin()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrganizationHandler(context);

        var orgId = await handler.Handle(
            new CreateOrganizationCommand("Test Choir", null, null, "user-1"),
            CancellationToken.None);

        var orgUser = await context.OrganizationUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(ou => ou.OrganizationId == orgId && ou.UserId == "user-1");

        orgUser.Should().NotBeNull();
        orgUser!.IsActive.Should().BeTrue();
        orgUser.PersonId.Should().NotBeNull();

        var adminRole = await context.Roles.IgnoreQueryFilters()
            .FirstAsync(r => r.OrganizationId == orgId && r.Name == "Admin");

        var assignment = await context.RoleAssignments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(ra => ra.RoleId == adminRole.Id && ra.PersonId == orgUser.PersonId);

        assignment.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenCreatingOrganizationThenExpenseCategoriesAreSeeded()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrganizationHandler(context);

        var orgId = await handler.Handle(
            new CreateOrganizationCommand("Test Choir", null, null, "user-1"),
            CancellationToken.None);

        var categories = await context.ExpenseCategories.IgnoreQueryFilters()
            .Where(ec => ec.OrganizationId == orgId)
            .Select(ec => ec.Name)
            .ToListAsync();

        categories.Should().HaveCount(8);
        categories.Should().Contain("SHEET MUSIC");
        categories.Should().Contain("OTHER");
    }
}
