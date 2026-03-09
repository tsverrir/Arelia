using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Organizations.Queries;

public record GetOrganizationUsersQuery(Guid OrganizationId) : IRequest<List<OrganizationUserDto>>;

public record OrganizationUserDto(
    Guid Id,
    string UserId,
    string? Email,
    string? PersonName,
    bool IsActive);

public class GetOrganizationUsersHandler(IAreliaDbContext context)
    : IRequestHandler<GetOrganizationUsersQuery, List<OrganizationUserDto>>
{
    public async Task<List<OrganizationUserDto>> Handle(
        GetOrganizationUsersQuery request, CancellationToken cancellationToken)
    {
        return await context.OrganizationUsers
            .IgnoreQueryFilters()
            .Where(ou => ou.OrganizationId == request.OrganizationId)
            .Select(ou => new OrganizationUserDto(
                ou.Id,
                ou.UserId,
                ou.Person != null ? ou.Person.Email : null,
                ou.Person != null ? ou.Person.FirstName + " " + ou.Person.LastName : null,
                ou.IsActive))
            .ToListAsync(cancellationToken);
    }
}
