using Arelia.Application.Interfaces;
using Arelia.Application.Organizations.Commands;
using Arelia.Domain.Common;
using Arelia.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Arelia.Application.Tests.Organizations;

public class InviteUserTests
{
    [Fact]
    public async Task WhenInvitingNewUserByEmailThenPersonAndOrgUserAreCreated()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var userService = Substitute.For<IUserService>();
        userService.FindUserIdByEmailAsync("new@test.dk").Returns((string?)null);
        userService.CreateUserAsync("new@test.dk").Returns(Result.Success("new-user-id"));
        userService.IsAccountPendingAsync("new-user-id").Returns(true);
        userService.SendInvitationEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var handler = new InviteUserHandler(context, userService);

        var result = await handler.Handle(
            new InviteUserCommand(orgId, "new@test.dk", "Alice", "Test", null, null, "Admin", "https://test.arelia.dev"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var person = await context.Persons.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Email == "new@test.dk");
        person.Should().NotBeNull();
        person!.FirstName.Should().Be("Alice");

        var orgUser = await context.OrganizationUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(ou => ou.UserId == "new-user-id" && ou.OrganizationId == orgId);
        orgUser.Should().NotBeNull();
        orgUser!.PersonId.Should().Be(person.Id);
    }

    [Fact]
    public async Task WhenInvitingExistingUserThenOnlyOrgUserIsCreated()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var userService = Substitute.For<IUserService>();
        userService.FindUserIdByEmailAsync("existing@test.dk").Returns("existing-user-id");
        userService.IsAccountPendingAsync("existing-user-id").Returns(false);
        userService.SendOrgAddedNotificationAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var handler = new InviteUserHandler(context, userService);

        var result = await handler.Handle(
            new InviteUserCommand(orgId, "existing@test.dk", "Bob", "Existing", null, null, "Admin", "https://test.arelia.dev"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await userService.DidNotReceive().CreateUserAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task WhenUserAlreadyLinkedThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId, "admin-1");

        var person = new Person { FirstName = "Ex", LastName = "User", OrganizationId = orgId };
        context.Persons.Add(person);
        context.OrganizationUsers.Add(new OrganizationUser
        {
            UserId = "existing-user-id",
            OrganizationId = orgId,
            PersonId = person.Id,
        });
        await context.SaveChangesAsync();

        var userService = Substitute.For<IUserService>();
        userService.FindUserIdByEmailAsync("existing@test.dk").Returns("existing-user-id");

        var handler = new InviteUserHandler(context, userService);

        var result = await handler.Handle(
            new InviteUserCommand(orgId, "existing@test.dk", null, null, null, null, "Admin", "https://test.arelia.dev"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already a member");
    }
}
