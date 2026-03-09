using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Roles.Commands;

public record CreateCustomRoleCommand(string Name, Guid OrganizationId) : IRequest<Domain.Common.Result<Guid>>;

public class CreateCustomRoleHandler(IAreliaDbContext context)
    : IRequestHandler<CreateCustomRoleCommand, Domain.Common.Result<Guid>>
{
    public async Task<Domain.Common.Result<Guid>> Handle(
        CreateCustomRoleCommand request, CancellationToken cancellationToken)
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
            RoleType = RoleType.Custom,
            OrganizationId = request.OrganizationId,
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync(cancellationToken);

        return Domain.Common.Result.Success(role.Id);
    }
}
