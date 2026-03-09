using Arelia.Application.Interfaces;
using Arelia.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Roles.Commands;

public record RemoveRoleAssignmentCommand(Guid AssignmentId) : IRequest<Result>;

public class RemoveRoleAssignmentHandler(IAreliaDbContext context)
    : IRequestHandler<RemoveRoleAssignmentCommand, Result>
{
    public async Task<Result> Handle(RemoveRoleAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await context.RoleAssignments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ra => ra.Id == request.AssignmentId, cancellationToken);

        if (assignment is null)
            return Result.Failure("Role assignment not found.");

        assignment.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
