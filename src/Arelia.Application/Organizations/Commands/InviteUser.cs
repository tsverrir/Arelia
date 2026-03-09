using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Organizations.Commands;

public record InviteUserCommand(
    string Email,
    Guid OrganizationId,
    string? FirstName,
    string? LastName) : IRequest<Domain.Common.Result>;

public class InviteUserHandler(
    IAreliaDbContext context,
    IUserService userService)
    : IRequestHandler<InviteUserCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var userId = await userService.FindUserIdByEmailAsync(request.Email);

        if (userId is not null)
        {
            var alreadyLinked = await context.OrganizationUsers
                .IgnoreQueryFilters()
                .AnyAsync(ou => ou.UserId == userId && ou.OrganizationId == request.OrganizationId,
                    cancellationToken);

            if (alreadyLinked)
                return Domain.Common.Result.Failure("User is already a member of this organization.");
        }
        else
        {
            var createResult = await userService.CreateUserAsync(request.Email);
            if (createResult.IsFailure)
                return Domain.Common.Result.Failure(createResult.Error!);
            userId = createResult.Value!;
        }

        var person = new Person
        {
            FirstName = request.FirstName ?? "New",
            LastName = request.LastName ?? "Member",
            Email = request.Email,
            OrganizationId = request.OrganizationId,
        };
        context.Persons.Add(person);

        var orgUser = new OrganizationUser
        {
            UserId = userId,
            OrganizationId = request.OrganizationId,
            PersonId = person.Id,
            IsActive = true,
        };
        context.OrganizationUsers.Add(orgUser);

        await context.SaveChangesAsync(cancellationToken);

        return Domain.Common.Result.Success();
    }
}
