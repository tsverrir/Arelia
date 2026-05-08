
namespace Arelia.Application.Roles.Commands;

public record UpdateRoleCommand(Guid RoleId, string Name) : IRequest<Domain.Common.Result>;

public class UpdateRoleHandler(IAreliaDbContext context)
    : IRequestHandler<UpdateRoleCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await context.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
            return Domain.Common.Result.Failure("Role not found.");

        var nameExists = await context.Roles
            .IgnoreQueryFilters()
            .AnyAsync(r => r.Id != request.RoleId &&
                           r.OrganizationId == role.OrganizationId &&
                           r.Name == request.Name &&
                           r.IsActive, cancellationToken);

        if (nameExists)
            return Domain.Common.Result.Failure("A role with this name already exists.");

        role.Name = request.Name;
        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
