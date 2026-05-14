// Copyright (c) 2026 JBT Marel. All rights reserved.
using Arelia.Application.People.Queries;
using Arelia.Application.Roles.Commands;
using Arelia.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Tests.Roles;

public class EndRoleAssignmentTests
{
    [Fact]
    public async Task WhenEndingTodayThenAssignmentIsImmediatelyInactive()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        var person = new Person { FirstName = "Anna", LastName = "Sig", OrganizationId = orgId };
        var role = new Role { Name = "Board", OrganizationId = orgId };
        context.Persons.Add(person);
        context.Roles.Add(role);

        var assignment = new RoleAssignment
        {
            PersonId = person.Id,
            RoleId = role.Id,
            OrganizationId = orgId,
            FromDate = DateTime.Today.AddDays(-5),
            ToDate = null
        };
        context.RoleAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new EndRoleAssignmentHandler(context);
        var result = await handler.Handle(
            new EndRoleAssignmentCommand(assignment.Id, DateTime.Today),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = await context.RoleAssignments
            .IgnoreQueryFilters()
            .FirstAsync(ra => ra.Id == assignment.Id);
        updated.ToDate.Should().Be(DateTime.Today);
        updated.IsCurrentlyActive.Should().BeFalse("a role ended today should not be active");
    }

    [Fact]
    public async Task WhenEndingTomorrowThenAssignmentIsStillActiveToday()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        var person = new Person { FirstName = "Jon", LastName = "Vik", OrganizationId = orgId };
        var role = new Role { Name = "Member", OrganizationId = orgId };
        context.Persons.Add(person);
        context.Roles.Add(role);

        var assignment = new RoleAssignment
        {
            PersonId = person.Id,
            RoleId = role.Id,
            OrganizationId = orgId,
            FromDate = DateTime.Today.AddDays(-5),
            ToDate = null
        };
        context.RoleAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var endHandler = new EndRoleAssignmentHandler(context);
        await endHandler.Handle(
            new EndRoleAssignmentCommand(assignment.Id, DateTime.Today.AddDays(1)),
            CancellationToken.None);

        var updated = await context.RoleAssignments
            .IgnoreQueryFilters()
            .FirstAsync(ra => ra.Id == assignment.Id);
        updated.IsCurrentlyActive.Should().BeTrue("a role ending tomorrow should still be active today");
    }

    [Fact]
    public async Task WhenEndingYesterdayThenAssignmentIsInactive()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        var person = new Person { FirstName = "Eva", LastName = "Berg", OrganizationId = orgId };
        var role = new Role { Name = "Admin", OrganizationId = orgId };
        context.Persons.Add(person);
        context.Roles.Add(role);

        var assignment = new RoleAssignment
        {
            PersonId = person.Id,
            RoleId = role.Id,
            OrganizationId = orgId,
            FromDate = DateTime.Today.AddDays(-10),
            ToDate = null
        };
        context.RoleAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new EndRoleAssignmentHandler(context);
        await handler.Handle(
            new EndRoleAssignmentCommand(assignment.Id, DateTime.Today.AddDays(-1)),
            CancellationToken.None);

        var updated = await context.RoleAssignments
            .IgnoreQueryFilters()
            .FirstAsync(ra => ra.Id == assignment.Id);
        updated.IsCurrentlyActive.Should().BeFalse("a role ended yesterday should not be active");
    }

    [Fact]
    public async Task WhenEndingTodayThenPersonDetailShowsAssignmentAsInactive()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        var person = new Person { FirstName = "Kari", LastName = "Dal", OrganizationId = orgId };
        var role = new Role { Name = "Board", OrganizationId = orgId };
        context.Persons.Add(person);
        context.Roles.Add(role);

        var assignment = new RoleAssignment
        {
            PersonId = person.Id,
            RoleId = role.Id,
            OrganizationId = orgId,
            FromDate = DateTime.Today.AddDays(-5),
            ToDate = null
        };
        context.RoleAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var endHandler = new EndRoleAssignmentHandler(context);
        await endHandler.Handle(
            new EndRoleAssignmentCommand(assignment.Id, DateTime.Today),
            CancellationToken.None);

        var detailHandler = new GetPersonDetailHandler(context);
        var detail = await detailHandler.Handle(new GetPersonDetailQuery(person.Id), CancellationToken.None);

        detail.Should().NotBeNull();
        var roleAssignment = detail!.RoleAssignments.FirstOrDefault(ra => ra.Id == assignment.Id);
        roleAssignment.Should().NotBeNull();
        roleAssignment!.IsCurrentlyActive.Should().BeFalse("role ended today should show as inactive in person detail");
    }
}
