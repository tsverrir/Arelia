using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Roles.Queries;

public record GetRolesQuery(Guid OrganizationId) : IRequest<List<RoleDto>>;

public record RoleDto(
    Guid Id,
    string Name,
    RoleType RoleType,
    bool IsActive,
    List<Permission> Permissions,
    int AssignmentCount);

public class GetRolesHandler(IAreliaDbContext context)
    : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        return await context.Roles
            .IgnoreQueryFilters()
            .Where(r => r.OrganizationId == request.OrganizationId)
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.RoleType,
                r.IsActive,
                r.RolePermissions.Where(rp => rp.IsActive).Select(rp => rp.Permission).ToList(),
                r.RoleAssignments.Count(ra => ra.IsActive && ra.FromDate <= DateTime.UtcNow && (ra.ToDate == null || ra.ToDate >= DateTime.UtcNow))))
            .ToListAsync(cancellationToken);
    }
}
