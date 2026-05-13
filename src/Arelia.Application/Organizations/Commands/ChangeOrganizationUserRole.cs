//-------------------------------------------------------------------------------------------------
//
// ChangeOrganizationUserRole.cs -- The ChangeOrganizationUserRoleCommand and handler.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------

using Arelia.Domain.Common;
using Arelia.Domain.Entities;

namespace Arelia.Application.Organizations.Commands;

/// <summary>
/// Atomically replaces all active role assignments for a user in an organisation with a single new role.
/// Ends all active assignments and creates a new one for the specified role.
/// </summary>
public record ChangeOrganizationUserRoleCommand(Guid OrganizationUserId, Guid RoleId) : IRequest<Result>;

//-----------------------------------------------------------------------------------------
/// <summary>
/// Handles <see cref="ChangeOrganizationUserRoleCommand"/>.
/// </summary>
public class ChangeOrganizationUserRoleHandler(IAreliaDbContext context)
    : IRequestHandler<ChangeOrganizationUserRoleCommand, Result>
{
    //-----------------------------------------------------------------------------------------
    /// <inheritdoc />
    public async Task<Result> Handle(
        ChangeOrganizationUserRoleCommand request, CancellationToken cancellationToken)
    {
        var orgUser = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                ou => ou.Id == request.OrganizationUserId,
                cancellationToken);

        if (orgUser is null)
        {
            return Result.Failure("Organization user not found.");
        }

        var roleExists = await context.Roles
            .IgnoreQueryFilters()
            .AnyAsync(
                r => r.Id == request.RoleId && r.OrganizationId == orgUser.OrganizationId,
                cancellationToken);

        if (!roleExists)
        {
            return Result.Failure("Role not found in this organization.");
        }

        var now = DateTime.UtcNow;

        var activeAssignments = await context.RoleAssignments
            .IgnoreQueryFilters()
            .Where(ra => ra.PersonId == orgUser.PersonId
                         && ra.OrganizationId == orgUser.OrganizationId
                         && (ra.ToDate == null || ra.ToDate > now))
            .ToListAsync(cancellationToken);

        foreach (var assignment in activeAssignments)
        {
            assignment.ToDate = now;
        }

        context.RoleAssignments.Add(new RoleAssignment
        {
            PersonId = orgUser.PersonId,
            RoleId = request.RoleId,
            OrganizationId = orgUser.OrganizationId,
            FromDate = now,
            ToDate = null,
        });

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
