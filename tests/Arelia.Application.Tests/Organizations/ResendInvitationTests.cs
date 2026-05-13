// Copyright (c) 2026 JBT Marel. All rights reserved.
using Arelia.Application.Interfaces;
using Arelia.Application.Organizations.Commands;
using Arelia.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Arelia.Application.Tests.Organizations;

public class ResendInvitationTests
{
    [Fact]
    public async Task WhenResendingToPendingUserThenEmailIsSent()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var org = new Organization { Id = orgId, Name = "Test Choir" };
        context.Organizations.Add(org);

        var person = new Person { FirstName = "Pending", LastName = "User", OrganizationId = orgId };
        context.Persons.Add(person);
        context.OrganizationUsers.Add(new OrganizationUser
        {
            UserId = "pending-user",
            OrganizationId = orgId,
            PersonId = person.Id,
        });
        await context.SaveChangesAsync();

        var userService = Substitute.For<IUserService>();
        userService.IsAccountPendingAsync("pending-user").Returns(true);
        userService.SendInvitationEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var handler = new ResendInvitationHandler(context, userService);
        var result = await handler.Handle(
            new ResendInvitationCommand("pending-user", orgId, "Admin User", "https://test.arelia.dev"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await userService.Received(1)
            .SendInvitationEmailAsync("pending-user", "Test Choir", "Admin User", "https://test.arelia.dev");
    }

    [Fact]
    public async Task WhenResendingToActiveUserThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var org = new Organization { Id = orgId, Name = "Test Choir" };
        context.Organizations.Add(org);

        var person = new Person { FirstName = "Active", LastName = "User", OrganizationId = orgId };
        context.Persons.Add(person);
        context.OrganizationUsers.Add(new OrganizationUser
        {
            UserId = "active-user",
            OrganizationId = orgId,
            PersonId = person.Id,
        });
        await context.SaveChangesAsync();

        var userService = Substitute.For<IUserService>();
        userService.IsAccountPendingAsync("active-user").Returns(false);

        var handler = new ResendInvitationHandler(context, userService);
        var result = await handler.Handle(
            new ResendInvitationCommand("active-user", orgId, "Admin User", "https://test.arelia.dev"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already active");
        await userService.DidNotReceive()
            .SendInvitationEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task WhenResendingToNonMemberThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var org = new Organization { Id = orgId, Name = "Test Choir" };
        context.Organizations.Add(org);
        await context.SaveChangesAsync();

        var userService = Substitute.For<IUserService>();

        var handler = new ResendInvitationHandler(context, userService);
        var result = await handler.Handle(
            new ResendInvitationCommand("non-member-id", orgId, "Admin User", "https://test.arelia.dev"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not a member");
    }
}
