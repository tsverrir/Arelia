using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Roles.Queries;

public record GetRoleDetailQuery(Guid RoleId) : IRequest<RoleDetailDto?>;

public record RoleDetailDto(
    Guid Id,
    string Name,
    bool IsActive,
    List<Permission> Permissions,
    List<RoleDetailAssignmentDto> ActiveAssignments);

public record RoleDetailAssignmentDto(
    Guid AssignmentId,
    Guid PersonId,
    string FirstName,
    string LastName,
    DateTime FromDate,
    DateTime? ToDate);

public class GetRoleDetailHandler(IAreliaDbContext context)
    : IRequestHandler<GetRoleDetailQuery, RoleDetailDto?>
{
    public async Task<RoleDetailDto?> Handle(GetRoleDetailQuery request, CancellationToken cancellationToken)
    {
        var role = await context.Roles
            .IgnoreQueryFilters()
            .Where(r => r.Id == request.RoleId)
            .Select(r => new RoleDetailDto(
                r.Id,
                r.Name,
                r.IsActive,
                r.RolePermissions
                    .Where(rp => rp.IsActive)
                    .Select(rp => rp.Permission)
                    .ToList(),
                r.RoleAssignments
                    .Where(ra => ra.IsActive &&
                                 ra.FromDate <= DateTime.UtcNow &&
                                 (ra.ToDate == null || ra.ToDate >= DateTime.UtcNow))
                    .OrderBy(ra => ra.Person.LastName)
                    .ThenBy(ra => ra.Person.FirstName)
                    .Select(ra => new RoleDetailAssignmentDto(
                        ra.Id,
                        ra.PersonId,
                        ra.Person.FirstName,
                        ra.Person.LastName,
                        ra.FromDate,
                        ra.ToDate))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return role;
    }
}
