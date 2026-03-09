using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Roles.Commands;

public enum RoleDeleteMode
{
    RemoveAssignments,
    TransferAssignments,
}

public record DeleteRoleCommand(
    Guid RoleId,
    RoleDeleteMode Mode,
    Guid? TransferToRoleId = null) : IRequest<Domain.Common.Result>;

public class DeleteRoleHandler(IAreliaDbContext context)
    : IRequestHandler<DeleteRoleCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await context.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
            return Domain.Common.Result.Failure("Role not found.");

        var activeAssignments = await context.RoleAssignments
            .Where(ra => ra.RoleId == request.RoleId &&
                         ra.IsActive &&
                         ra.FromDate <= DateTime.UtcNow &&
                         (ra.ToDate == null || ra.ToDate >= DateTime.UtcNow))
            .ToListAsync(cancellationToken);

        if (activeAssignments.Count > 0)
        {
            if (request.Mode == RoleDeleteMode.TransferAssignments)
            {
                if (request.TransferToRoleId is not Guid targetId)
                    return Domain.Common.Result.Failure("A target role must be specified for transfer.");

                var targetExists = await context.Roles
                    .IgnoreQueryFilters()
                    .AnyAsync(r => r.Id == targetId && r.IsActive, cancellationToken);

                if (!targetExists)
                    return Domain.Common.Result.Failure("Target role not found or is inactive.");

                foreach (var assignment in activeAssignments)
                    assignment.RoleId = targetId;
            }
            else
            {
                foreach (var assignment in activeAssignments)
                    assignment.IsActive = false;
            }
        }

        role.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
