using Arelia.Domain.Enums;

namespace Arelia.Application.Authorization;

public interface IPermissionService
{
    Task<HashSet<Permission>> GetEffectivePermissionsAsync(string userId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> IsActiveMemberAsync(string userId, Guid organizationId, CancellationToken cancellationToken = default);
}

public class PermissionService(IAreliaDbContext context) : IPermissionService
{
    /// <summary>
    /// A member is active if they have an OrganizationUser record and at least one active role assignment.
    /// </summary>
    public async Task<bool> IsActiveMemberAsync(string userId, Guid organizationId, CancellationToken cancellationToken)
    {
        var orgUser = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ou => ou.UserId == userId && ou.OrganizationId == organizationId,
                cancellationToken);

        if (orgUser is null)
            return false;

        var now = DateTime.UtcNow;
        return await context.RoleAssignments
            .IgnoreQueryFilters()
            .AnyAsync(ra => ra.PersonId == orgUser.PersonId
                            && ra.OrganizationId == organizationId
                            && ra.IsActive
                            && ra.FromDate <= now
                            && (ra.ToDate == null || ra.ToDate >= now),
                cancellationToken);
    }

    public async Task<HashSet<Permission>> GetEffectivePermissionsAsync(
        string userId, Guid organizationId, CancellationToken cancellationToken)
    {
        var orgUser = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ou => ou.UserId == userId && ou.OrganizationId == organizationId,
                cancellationToken);

        if (orgUser is null)
            return [];

        var now = DateTime.UtcNow;

        var permissions = await context.RoleAssignments
            .IgnoreQueryFilters()
            .Where(ra =>
                ra.PersonId == orgUser.PersonId &&
                ra.OrganizationId == organizationId &&
                ra.IsActive &&
                ra.FromDate <= now &&
                (ra.ToDate == null || ra.ToDate >= now))
            .SelectMany(ra => ra.Role.RolePermissions
                .Where(rp => rp.IsActive)
                .Select(rp => rp.Permission))
            .Distinct()
            .ToListAsync(cancellationToken);

        return permissions.ToHashSet();
    }
}
