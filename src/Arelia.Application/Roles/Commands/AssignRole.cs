using Arelia.Domain.Entities;

namespace Arelia.Application.Roles.Commands;

public record AssignRoleCommand(
    Guid PersonId,
    Guid RoleId,
    DateTime FromDate,
    DateTime? ToDate,
    Guid OrganizationId) : IRequest<Domain.Common.Result>;

public class AssignRoleHandler(IAreliaDbContext context)
    : IRequestHandler<AssignRoleCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var person = await context.Persons
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == request.PersonId && p.OrganizationId == request.OrganizationId, cancellationToken);

        if (!person)
            return Domain.Common.Result.Failure("Person not found in this organization.");

        var role = await context.Roles
            .IgnoreQueryFilters()
            .AnyAsync(r => r.Id == request.RoleId && r.OrganizationId == request.OrganizationId, cancellationToken);

        if (!role)
            return Domain.Common.Result.Failure("Role not found in this organization.");

        context.RoleAssignments.Add(new RoleAssignment
        {
            PersonId = request.PersonId,
            RoleId = request.RoleId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            OrganizationId = request.OrganizationId,
        });

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
