using Arelia.Domain.Common;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;

namespace Arelia.Application.Organizations.Commands;

/// <summary>
/// Directly assigns an existing system user to an organisation (System Admin only).
/// A minimal Person record is created; no invitation email is sent.
/// </summary>
public record DirectAssignUserCommand(
    string UserId,
    Guid OrganizationId,
    Guid? RoleId = null) : IRequest<Result>;

public class DirectAssignUserHandler(IAreliaDbContext context, IUserService userService)
    : IRequestHandler<DirectAssignUserCommand, Result>
{
    public async Task<Result> Handle(DirectAssignUserCommand request, CancellationToken cancellationToken)
    {
        var alreadyMember = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(ou => ou.UserId == request.UserId
                            && ou.OrganizationId == request.OrganizationId, cancellationToken);

        if (alreadyMember)
            return Result.Failure("User is already a member of this organisation.");

        var displayName = await userService.GetUserDisplayNameAsync(request.UserId);
        var nameParts = (displayName ?? "New Member").Split(' ', 2);

        var person = new Person
        {
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            OrganizationId = request.OrganizationId,
        };
        context.Persons.Add(person);

        var orgUser = new OrganizationUser
        {
            UserId = request.UserId,
            OrganizationId = request.OrganizationId,
            PersonId = person.Id,
        };
        context.OrganizationUsers.Add(orgUser);

        Role? role = null;

        if (request.RoleId.HasValue)
        {
            role = await context.Roles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == request.RoleId.Value
                                          && r.OrganizationId == request.OrganizationId, cancellationToken);
        }

        role ??= await context.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.OrganizationId == request.OrganizationId
                                      && r.RoleType == RoleType.Member
                                      && r.IsActive, cancellationToken);

        if (role is not null)
        {
            context.RoleAssignments.Add(new RoleAssignment
            {
                PersonId = person.Id,
                RoleId = role.Id,
                FromDate = DateTime.UtcNow,
                OrganizationId = request.OrganizationId,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
