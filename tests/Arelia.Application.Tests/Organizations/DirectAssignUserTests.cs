// Copyright (c) 2026 JBT Marel. All rights reserved.
using Arelia.Application.Interfaces;
using Arelia.Application.Organizations.Commands;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Arelia.Application.Tests.Organizations;

public class DirectAssignUserTests
{
    [Fact]
    public async Task WhenDirectAssigningUserThenPersonAndOrgUserAreCreated()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var role = new Role { Name = "Member", RoleType = RoleType.Member, OrganizationId = orgId, IsActive = true };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var userService = Substitute.For<IUserService>();
        userService.GetUserDisplayNameAsync("user-x").Returns("Maria Karlsson");

        var handler = new DirectAssignUserHandler(context, userService);
        var result = await handler.Handle(
            new DirectAssignUserCommand("user-x", orgId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var person = await context.Persons.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.OrganizationId == orgId);
        person.Should().NotBeNull();
        person!.FirstName.Should().Be("Maria");
        person.LastName.Should().Be("Karlsson");

        var orgUser = await context.OrganizationUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(ou => ou.UserId == "user-x");
        orgUser.Should().NotBeNull();
        orgUser!.PersonId.Should().Be(person.Id);
    }

    [Fact]
    public async Task WhenDirectAssigningUserThenDefaultMemberRoleIsAssigned()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var memberRole = new Role { Name = "Member", RoleType = RoleType.Member, OrganizationId = orgId, IsActive = true };
        context.Roles.Add(memberRole);
        await context.SaveChangesAsync();

        var userService = Substitute.For<IUserService>();
        userService.GetUserDisplayNameAsync("user-y").Returns("Test User");

        var handler = new DirectAssignUserHandler(context, userService);
        await handler.Handle(new DirectAssignUserCommand("user-y", orgId), CancellationToken.None);

        var person = await context.Persons.IgnoreQueryFilters()
            .FirstAsync(p => p.OrganizationId == orgId);

        var assignment = await context.RoleAssignments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(ra => ra.PersonId == person.Id);
        assignment.Should().NotBeNull();
        assignment!.RoleId.Should().Be(memberRole.Id);
    }

    [Fact]
    public async Task WhenDirectAssigningUserThenNoEmailIsSent()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var userService = Substitute.For<IUserService>();
        userService.GetUserDisplayNameAsync("user-z").Returns("Silent User");

        var handler = new DirectAssignUserHandler(context, userService);
        await handler.Handle(new DirectAssignUserCommand("user-z", orgId), CancellationToken.None);

        await userService.DidNotReceive()
            .SendInvitationEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await userService.DidNotReceive()
            .SendOrgAddedNotificationAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task WhenDirectAssigningAlreadyMemberUserThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var person = new Person { FirstName = "Existing", LastName = "Member", OrganizationId = orgId };
        context.Persons.Add(person);
        context.OrganizationUsers.Add(new OrganizationUser
        {
            UserId = "user-dupe",
            OrganizationId = orgId,
            PersonId = person.Id,
        });
        await context.SaveChangesAsync();

        var userService = Substitute.For<IUserService>();
        userService.GetUserDisplayNameAsync("user-dupe").Returns("Existing Member");

        var handler = new DirectAssignUserHandler(context, userService);
        var result = await handler.Handle(
            new DirectAssignUserCommand("user-dupe", orgId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already a member");
    }
}
