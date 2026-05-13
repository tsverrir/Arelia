using Arelia.Domain.Common;

namespace Arelia.Application.Organizations.Commands;

/// <summary>
/// Suspends a user by ending all their active role assignments.
/// The OrganizationUser record is preserved; reinstatement is done by assigning new roles.
/// </summary>
public record SuspendOrganizationUserCommand(Guid OrganizationUserId) : IRequest<Result>;

public class SuspendOrganizationUserHandler(IAreliaDbContext context)
    : IRequestHandler<SuspendOrganizationUserCommand, Result>
{
    public async Task<Result> Handle(
        SuspendOrganizationUserCommand request, CancellationToken cancellationToken)
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

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
