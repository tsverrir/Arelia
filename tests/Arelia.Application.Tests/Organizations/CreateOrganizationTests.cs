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
            new CreateOrganizationCommand("Test Choir", "test@choir.dk", "+45 1234"),
            CancellationToken.None);

        var org = await context.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == orgId);
        org.Should().NotBeNull();
        org!.Name.Should().Be("Test Choir");
        org.ContactEmail.Should().Be("test@choir.dk");
        org.DefaultCurrencyCode.Should().Be("DKK");
    }

    [Fact]
    public async Task WhenCreatingOrganizationThenDefaultRolesAreSeeded()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrganizationHandler(context);

        var orgId = await handler.Handle(
            new CreateOrganizationCommand("Test Choir", null, null),
            CancellationToken.None);

        var roles = await context.Roles.IgnoreQueryFilters()
            .Where(r => r.OrganizationId == orgId)
            .ToListAsync();

        roles.Should().HaveCount(5);
        roles.Select(r => r.Name).Should().Contain(new[] { "Member", "Board", "Treasurer", "Conductor", "Admin" });
    }

    [Fact]
    public async Task WhenCreatingOrganizationThenSystemRolesHaveCorrectRoleTypes()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrganizationHandler(context);

        var orgId = await handler.Handle(
            new CreateOrganizationCommand("Test Choir", null, null),
            CancellationToken.None);

        var roles = await context.Roles.IgnoreQueryFilters()
            .Where(r => r.OrganizationId == orgId)
            .ToListAsync();

        roles.First(r => r.Name == "Admin").RoleType.Should().Be(RoleType.Admin);
        roles.First(r => r.Name == "Board").RoleType.Should().Be(RoleType.Board);
        roles.First(r => r.Name == "Member").RoleType.Should().Be(RoleType.Member);
        roles.First(r => r.Name == "Treasurer").RoleType.Should().Be(RoleType.Custom);
        roles.First(r => r.Name == "Conductor").RoleType.Should().Be(RoleType.Custom);
    }

    [Fact]
    public async Task WhenCreatingOrganizationThenAdminRoleHasAllPermissions()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrganizationHandler(context);

        var orgId = await handler.Handle(
            new CreateOrganizationCommand("Test Choir", null, null),
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
    public async Task WhenCreatingOrganizationThenNoUsersAreAutoLinked()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrganizationHandler(context);

        var orgId = await handler.Handle(
            new CreateOrganizationCommand("Test Choir", null, null),
            CancellationToken.None);

        var orgUsers = await context.OrganizationUsers.IgnoreQueryFilters()
            .Where(ou => ou.OrganizationId == orgId)
            .ToListAsync();

        orgUsers.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenCreatingOrganizationThenExpenseCategoriesAreSeeded()
    {
        await using var context = TestDbContextFactory.Create();
        var handler = new CreateOrganizationHandler(context);

        var orgId = await handler.Handle(
            new CreateOrganizationCommand("Test Choir", null, null),
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
