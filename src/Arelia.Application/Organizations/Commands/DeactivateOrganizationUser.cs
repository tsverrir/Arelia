using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Organizations.Commands;

public record DeactivateOrganizationUserCommand(Guid OrganizationUserId) : IRequest<Domain.Common.Result>;

public class DeactivateOrganizationUserHandler(IAreliaDbContext context)
    : IRequestHandler<DeactivateOrganizationUserCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        DeactivateOrganizationUserCommand request, CancellationToken cancellationToken)
    {
        var orgUser = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ou => ou.Id == request.OrganizationUserId, cancellationToken);

        if (orgUser is null)
            return Domain.Common.Result.Failure("Organization user not found.");

        orgUser.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);

        return Domain.Common.Result.Success();
    }
}
