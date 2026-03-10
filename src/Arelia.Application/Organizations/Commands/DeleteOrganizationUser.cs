using Arelia.Application.Interfaces;
using Arelia.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Organizations.Commands;

public record DeleteOrganizationUserCommand(Guid OrganizationUserId) : IRequest<Result>;

/// <summary>
/// Permanently removes an organization-user link and its associated person record.
/// </summary>
public class DeleteOrganizationUserHandler(IAreliaDbContext context)
    : IRequestHandler<DeleteOrganizationUserCommand, Result>
{
    public async Task<Result> Handle(
        DeleteOrganizationUserCommand request, CancellationToken cancellationToken)
    {
        var orgUser = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ou => ou.Id == request.OrganizationUserId, cancellationToken);

        if (orgUser is null)
            return Result.Failure("Organization user not found.");

        // Remove role assignments for the linked person
        if (orgUser.PersonId.HasValue)
        {
            var roleAssignments = await context.RoleAssignments
                .IgnoreQueryFilters()
                .Where(ra => ra.PersonId == orgUser.PersonId.Value)
                .ToListAsync(cancellationToken);

            context.RoleAssignments.RemoveRange(roleAssignments);

            var person = await context.Persons
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == orgUser.PersonId.Value, cancellationToken);

            if (person is not null)
            {
                person.IsDeleted = true;
                person.IsActive = false;
            }
        }

        context.OrganizationUsers.Remove(orgUser);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
