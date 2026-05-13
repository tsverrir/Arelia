// Copyright (c) 2026 JBT Marel. All rights reserved.
using Arelia.Application.Organizations.Commands;
using Arelia.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Tests.Organizations;

public class SuspendOrganizationUserTests
{
    [Fact]
    public async Task WhenSuspendingUserThenActiveRoleAssignmentsAreEnded()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var person = new Person { FirstName = "Jan", LastName = "Test", OrganizationId = orgId };
        context.Persons.Add(person);

        var orgUser = new OrganizationUser
        {
            UserId = "user-1",
            OrganizationId = orgId,
            PersonId = person.Id,
        };
        context.OrganizationUsers.Add(orgUser);

        var role = new Role { Name = "Member", OrganizationId = orgId };
        context.Roles.Add(role);

        var assignment = new RoleAssignment
        {
            PersonId = person.Id,
            RoleId = role.Id,
            OrganizationId = orgId,
            FromDate = DateTime.UtcNow.AddDays(-30),
        };
        context.RoleAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new SuspendOrganizationUserHandler(context);
        var result = await handler.Handle(
            new SuspendOrganizationUserCommand(orgUser.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = await context.RoleAssignments.IgnoreQueryFilters()
            .FirstAsync(ra => ra.Id == assignment.Id);
        updated.ToDate.Should().NotBeNull();
        updated.ToDate!.Value.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task WhenSuspendingUserThenOrganizationUserRecordIsPreserved()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var person = new Person { FirstName = "Jan", LastName = "Test", OrganizationId = orgId };
        context.Persons.Add(person);

        var orgUser = new OrganizationUser
        {
            UserId = "user-1",
            OrganizationId = orgId,
            PersonId = person.Id,
        };
        context.OrganizationUsers.Add(orgUser);
        await context.SaveChangesAsync();

        var handler = new SuspendOrganizationUserHandler(context);
        await handler.Handle(new SuspendOrganizationUserCommand(orgUser.Id), CancellationToken.None);

        var stillExists = await context.OrganizationUsers.IgnoreQueryFilters()
            .AnyAsync(ou => ou.Id == orgUser.Id);
        stillExists.Should().BeTrue();
    }

    [Fact]
    public async Task WhenSuspendingAlreadySuspendedUserThenSucceedsAsNoOp()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var person = new Person { FirstName = "Jan", LastName = "Test", OrganizationId = orgId };
        context.Persons.Add(person);

        var orgUser = new OrganizationUser
        {
            UserId = "user-1",
            OrganizationId = orgId,
            PersonId = person.Id,
        };
        context.OrganizationUsers.Add(orgUser);
        await context.SaveChangesAsync();

        var handler = new SuspendOrganizationUserHandler(context);
        var result = await handler.Handle(
            new SuspendOrganizationUserCommand(orgUser.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task WhenSuspendingNonExistentUserThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var handler = new SuspendOrganizationUserHandler(context);
        var result = await handler.Handle(
            new SuspendOrganizationUserCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
