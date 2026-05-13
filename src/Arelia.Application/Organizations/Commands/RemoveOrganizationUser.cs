using Arelia.Domain.Common;

namespace Arelia.Application.Organizations.Commands;

/// <summary>
/// Removes a user from an organisation. The Person record and historical data are preserved.
/// All active role assignments are ended. The OrganizationUser record is deleted.
/// </summary>
public record RemoveOrganizationUserCommand(Guid OrganizationUserId) : IRequest<Result>;

public class RemoveOrganizationUserHandler(IAreliaDbContext context)
    : IRequestHandler<RemoveOrganizationUserCommand, Result>
{
    public async Task<Result> Handle(
        RemoveOrganizationUserCommand request, CancellationToken cancellationToken)
    {
        var orgUser = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ou => ou.Id == request.OrganizationUserId, cancellationToken);

        if (orgUser is null)
            return Result.Failure("Organization user not found.");

        var activeAssignments = await context.RoleAssignments
            .IgnoreQueryFilters()
            .Where(ra => ra.PersonId == orgUser.PersonId
                         && ra.OrganizationId == orgUser.OrganizationId
                         && (ra.ToDate == null || ra.ToDate > DateTime.UtcNow))
            .ToListAsync(cancellationToken);

        foreach (var assignment in activeAssignments)
            assignment.ToDate = DateTime.UtcNow;

        context.OrganizationUsers.Remove(orgUser);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
