using Arelia.Application.Interfaces;
using Arelia.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Roles.Commands;

public record EndRoleAssignmentCommand(Guid AssignmentId, DateTime ToDate) : IRequest<Result>;

public class EndRoleAssignmentHandler(IAreliaDbContext context)
    : IRequestHandler<EndRoleAssignmentCommand, Result>
{
    public async Task<Result> Handle(EndRoleAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await context.RoleAssignments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ra => ra.Id == request.AssignmentId, cancellationToken);

        if (assignment is null)
            return Result.Failure("Role assignment not found.");

        assignment.ToDate = request.ToDate;
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
