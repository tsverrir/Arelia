using Arelia.Application.Authorization;
using Arelia.Application.Organizations.Commands;
using Arelia.Application.Tests;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Tests.Authorization;

public class PermissionServiceTests
{
    [Fact]
    public async Task WhenUserIsInactiveThenIsActiveMemberReturnsFalse()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create();

        context.OrganizationUsers.Add(new OrganizationUser
        {
            UserId = "user-1",
            OrganizationId = orgId,
            IsActive = false,
        });
        await context.SaveChangesAsync();

        var service = new PermissionService(context);
        var result = await service.IsActiveMemberAsync("user-1", orgId, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task WhenUserIsActiveThenIsActiveMemberReturnsTrue()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create();

        context.OrganizationUsers.Add(new OrganizationUser
        {
            UserId = "user-1",
            OrganizationId = orgId,
            IsActive = true,
        });
        await context.SaveChangesAsync();

        var service = new PermissionService(context);
        var result = await service.IsActiveMemberAsync("user-1", orgId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenUserHasAdminRoleThenAllPermissionsReturned()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create();

        // Create org via handler (seeds roles)
        var createHandler = new CreateOrganizationHandler(context);
        await createHandler.Handle(
            new CreateOrganizationCommand("Test", null, null, "user-1"),
            CancellationToken.None);

        var service = new PermissionService(context);
        var permissions = await service.GetEffectivePermissionsAsync("user-1", orgId, CancellationToken.None);

        // user-1 should have all permissions because CreateOrganization assigns Admin
        // but the org created by handler has its own orgId, not our variable
        // Let's query the actual orgId
        var actualOrg = await context.Organizations.IgnoreQueryFilters().FirstAsync();
        permissions = await service.GetEffectivePermissionsAsync("user-1", actualOrg.Id, CancellationToken.None);

        permissions.Should().HaveCount(Enum.GetValues<Permission>().Length);
    }

    [Fact]
    public async Task WhenUserHasNoRoleThenNoPermissionsReturned()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create();

        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            OrganizationId = orgId,
        };
        context.Persons.Add(person);

        context.OrganizationUsers.Add(new OrganizationUser
        {
            UserId = "user-1",
            OrganizationId = orgId,
            PersonId = person.Id,
            IsActive = true,
        });
        await context.SaveChangesAsync();

        var service = new PermissionService(context);
        var permissions = await service.GetEffectivePermissionsAsync("user-1", orgId, CancellationToken.None);

        permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenMultipleRolesThenPermissionsAreAdditive()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create();

        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            OrganizationId = orgId,
        };
        context.Persons.Add(person);

        context.OrganizationUsers.Add(new OrganizationUser
        {
            UserId = "user-1",
            OrganizationId = orgId,
            PersonId = person.Id,
            IsActive = true,
        });

        var boardRole = new Role { Name = "Board", RoleType = RoleType.System, OrganizationId = orgId };
        var conductorRole = new Role { Name = "Conductor", RoleType = RoleType.System, OrganizationId = orgId };
        context.Roles.AddRange(boardRole, conductorRole);

        context.RolePermissions.Add(new RolePermission
        {
            RoleId = boardRole.Id,
            Permission = Permission.ManagePeople,
            OrganizationId = orgId,
        });
        context.RolePermissions.Add(new RolePermission
        {
            RoleId = conductorRole.Id,
            Permission = Permission.ManageAttendance,
            OrganizationId = orgId,
        });

        context.RoleAssignments.Add(new RoleAssignment
        {
            PersonId = person.Id,
            RoleId = boardRole.Id,
            FromDate = DateTime.UtcNow.AddDays(-1),
            OrganizationId = orgId,
        });
        context.RoleAssignments.Add(new RoleAssignment
        {
            PersonId = person.Id,
            RoleId = conductorRole.Id,
            FromDate = DateTime.UtcNow.AddDays(-1),
            OrganizationId = orgId,
        });

        await context.SaveChangesAsync();

        var service = new PermissionService(context);
        var permissions = await service.GetEffectivePermissionsAsync("user-1", orgId, CancellationToken.None);

        permissions.Should().Contain(Permission.ManagePeople);
        permissions.Should().Contain(Permission.ManageAttendance);
    }

    [Fact]
    public async Task WhenRoleAssignmentExpiredThenPermissionsNotGranted()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create();

        var person = new Person
        {
            FirstName = "Test",
            LastName = "User",
            OrganizationId = orgId,
        };
        context.Persons.Add(person);

        context.OrganizationUsers.Add(new OrganizationUser
        {
            UserId = "user-1",
            OrganizationId = orgId,
            PersonId = person.Id,
            IsActive = true,
        });

        var role = new Role { Name = "Board", RoleType = RoleType.System, OrganizationId = orgId };
        context.Roles.Add(role);
        context.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            Permission = Permission.ManagePeople,
            OrganizationId = orgId,
        });

        // Expired assignment
        context.RoleAssignments.Add(new RoleAssignment
        {
            PersonId = person.Id,
            RoleId = role.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow.AddDays(-5),
            OrganizationId = orgId,
        });

        await context.SaveChangesAsync();

        var service = new PermissionService(context);
        var permissions = await service.GetEffectivePermissionsAsync("user-1", orgId, CancellationToken.None);

        permissions.Should().BeEmpty();
    }
}
