// Copyright (c) 2026 JBT Marel. All rights reserved.
using Arelia.Application.Organizations.Commands;
using Arelia.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Tests.Organizations;

public class RemoveOrganizationUserTests
{
    [Fact]
    public async Task WhenRemovingUserThenOrganizationUserIsDeleted()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var person = new Person { FirstName = "Bob", LastName = "Remove", OrganizationId = orgId };
        context.Persons.Add(person);

        var orgUser = new OrganizationUser
        {
            UserId = "user-remove-1",
            OrganizationId = orgId,
            PersonId = person.Id,
        };
        context.OrganizationUsers.Add(orgUser);
        await context.SaveChangesAsync();

        var handler = new RemoveOrganizationUserHandler(context);
        var result = await handler.Handle(
            new RemoveOrganizationUserCommand(orgUser.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var exists = await context.OrganizationUsers.IgnoreQueryFilters()
            .AnyAsync(ou => ou.Id == orgUser.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task WhenRemovingUserThenPersonRecordIsPreserved()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var person = new Person { FirstName = "Bob", LastName = "Remove", OrganizationId = orgId };
        context.Persons.Add(person);

        var orgUser = new OrganizationUser
        {
            UserId = "user-remove-2",
            OrganizationId = orgId,
            PersonId = person.Id,
        };
        context.OrganizationUsers.Add(orgUser);
        await context.SaveChangesAsync();

        var handler = new RemoveOrganizationUserHandler(context);
        await handler.Handle(new RemoveOrganizationUserCommand(orgUser.Id), CancellationToken.None);

        var personStillExists = await context.Persons.IgnoreQueryFilters()
            .AnyAsync(p => p.Id == person.Id);
        personStillExists.Should().BeTrue();
    }

    [Fact]
    public async Task WhenRemovingUserThenActiveRoleAssignmentsAreEnded()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var person = new Person { FirstName = "Bob", LastName = "Remove", OrganizationId = orgId };
        context.Persons.Add(person);

        var role = new Role { Name = "Member", OrganizationId = orgId };
        context.Roles.Add(role);

        var orgUser = new OrganizationUser
        {
            UserId = "user-remove-3",
            OrganizationId = orgId,
            PersonId = person.Id,
        };
        context.OrganizationUsers.Add(orgUser);

        var assignment = new RoleAssignment
        {
            PersonId = person.Id,
            RoleId = role.Id,
            OrganizationId = orgId,
            FromDate = DateTime.UtcNow.AddDays(-10),
        };
        context.RoleAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new RemoveOrganizationUserHandler(context);
        await handler.Handle(new RemoveOrganizationUserCommand(orgUser.Id), CancellationToken.None);

        var updatedAssignment = await context.RoleAssignments.IgnoreQueryFilters()
            .FirstAsync(ra => ra.Id == assignment.Id);
        updatedAssignment.ToDate.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenRemovingNonExistentUserThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var handler = new RemoveOrganizationUserHandler(context);
        var result = await handler.Handle(
            new RemoveOrganizationUserCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
