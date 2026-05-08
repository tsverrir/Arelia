using Arelia.Domain.Entities;

namespace Arelia.Application.Organizations.Queries;

public record GetUserOrganizationsQuery(string UserId) : IRequest<List<OrganizationDto>>;

public record OrganizationDto(Guid Id, string Name, bool IsActive);

public class GetUserOrganizationsHandler(IAreliaDbContext context)
    : IRequestHandler<GetUserOrganizationsQuery, List<OrganizationDto>>
{
    public async Task<List<OrganizationDto>> Handle(
        GetUserOrganizationsQuery request, CancellationToken cancellationToken)
    {
        return await context.OrganizationUsers
            .Where(ou => ou.UserId == request.UserId && ou.IsActive)
            .Select(ou => new OrganizationDto(
                ou.Organization.Id,
                ou.Organization.Name,
                ou.Organization.IsActive))
            .ToListAsync(cancellationToken);
    }
}
