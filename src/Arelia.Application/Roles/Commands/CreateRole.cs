using Arelia.Domain.Entities;

namespace Arelia.Application.Roles.Commands;

public record CreateRoleCommand(string Name, Guid OrganizationId) : IRequest<Domain.Common.Result<Guid>>;

public class CreateRoleHandler(IAreliaDbContext context)
    : IRequestHandler<CreateRoleCommand, Domain.Common.Result<Guid>>
{
    public async Task<Domain.Common.Result<Guid>> Handle(
        CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var exists = await context.Roles
            .IgnoreQueryFilters()
            .AnyAsync(r => r.OrganizationId == request.OrganizationId && r.Name == request.Name && r.IsActive,
                cancellationToken);

        if (exists)
            return Domain.Common.Result.Failure<Guid>("A role with this name already exists.");

        var role = new Role
        {
            Name = request.Name,
            OrganizationId = request.OrganizationId,
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync(cancellationToken);

        return Domain.Common.Result.Success(role.Id);
    }
}
