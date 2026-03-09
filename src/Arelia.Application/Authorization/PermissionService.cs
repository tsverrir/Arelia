using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Authorization;

public interface IPermissionService
{
    Task<HashSet<Permission>> GetEffectivePermissionsAsync(string userId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> IsActiveMemberAsync(string userId, Guid organizationId, CancellationToken cancellationToken = default);
}

public class PermissionService(IAreliaDbContext context) : IPermissionService
{
    public async Task<bool> IsActiveMemberAsync(string userId, Guid organizationId, CancellationToken cancellationToken)
    {
        return await context.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(ou => ou.UserId == userId && ou.OrganizationId == organizationId && ou.IsActive,
                cancellationToken);
    }

    public async Task<HashSet<Permission>> GetEffectivePermissionsAsync(
        string userId, Guid organizationId, CancellationToken cancellationToken)
    {
        var orgUser = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ou => ou.UserId == userId && ou.OrganizationId == organizationId && ou.IsActive,
                cancellationToken);

        if (orgUser?.PersonId is null)
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
