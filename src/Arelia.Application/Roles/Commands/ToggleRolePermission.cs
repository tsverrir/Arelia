using Arelia.Domain.Entities;
using Arelia.Domain.Enums;

namespace Arelia.Application.Roles.Commands;

public record ToggleRolePermissionCommand(
    Guid RoleId,
    Permission Permission,
    bool Grant,
    Guid OrganizationId) : IRequest<Domain.Common.Result>;

public class ToggleRolePermissionHandler(IAreliaDbContext context)
    : IRequestHandler<ToggleRolePermissionCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        ToggleRolePermissionCommand request, CancellationToken cancellationToken)
    {
        var existing = await context.RolePermissions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(rp =>
                rp.RoleId == request.RoleId && rp.Permission == request.Permission,
                cancellationToken);

        if (request.Grant)
        {
            if (existing is not null && existing.IsActive)
                return Domain.Common.Result.Success();

            if (existing is not null)
            {
                existing.IsActive = true;
            }
            else
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = request.RoleId,
                    Permission = request.Permission,
                    OrganizationId = request.OrganizationId,
                });
            }
        }
        else
        {
            if (existing is null || !existing.IsActive)
                return Domain.Common.Result.Success();

            existing.IsActive = false;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
