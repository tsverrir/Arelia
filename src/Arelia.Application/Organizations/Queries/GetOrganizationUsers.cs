using Arelia.Domain.Entities;

namespace Arelia.Application.Organizations.Queries;

public record GetOrganizationUsersQuery(Guid OrganizationId) : IRequest<List<OrganizationUserDto>>;

public record OrganizationUserDto(
    Guid Id,
    string UserId,
    string? Email,
    string? PersonName,
    Guid PersonId,
    bool IsPending,
    List<string> ActiveRoles);

public class GetOrganizationUsersHandler(IAreliaDbContext context, IUserService userService)
    : IRequestHandler<GetOrganizationUsersQuery, List<OrganizationUserDto>>
{
    public async Task<List<OrganizationUserDto>> Handle(
        GetOrganizationUsersQuery request, CancellationToken cancellationToken)
    {
        var orgUsers = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .Where(ou => ou.OrganizationId == request.OrganizationId)
            .Select(ou => new
            {
                ou.Id,
                ou.UserId,
                ou.PersonId,
                PersonName = ou.Person.FirstName + " " + ou.Person.LastName,
                Email = ou.Person.Email,
                ActiveRoles = context.RoleAssignments
                    .IgnoreQueryFilters()
                    .Where(ra => ra.PersonId == ou.PersonId
                                 && ra.OrganizationId == request.OrganizationId
                                 && (ra.ToDate == null || ra.ToDate > DateTime.UtcNow))
                    .Join(context.Roles.IgnoreQueryFilters(),
                        ra => ra.RoleId, r => r.Id,
                        (ra, r) => r.Name)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var result = new List<OrganizationUserDto>(orgUsers.Count);

        foreach (var ou in orgUsers)
        {
            var isPending = await userService.IsAccountPendingAsync(ou.UserId);
            result.Add(new OrganizationUserDto(
                ou.Id,
                ou.UserId,
                ou.Email,
                ou.PersonName,
                ou.PersonId,
                isPending,
                ou.ActiveRoles));
        }

        return result;
    }
}
