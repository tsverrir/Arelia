
namespace Arelia.Application.People.Queries;

public record GetPeopleQuery(
    Guid OrganizationId,
    Guid? VoiceGroupIdFilter = null,
    bool? IsActiveFilter = null,
    string? SearchTerm = null) : IRequest<List<PersonListDto>>;

public record PersonListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    Guid? VoiceGroupId,
    string? VoiceGroupName,
    bool IsActive,
    bool IsSuspended,
    bool IsPending,
    List<string> Roles);

public class GetPeopleHandler(IAreliaDbContext context, IUserService userService)
    : IRequestHandler<GetPeopleQuery, List<PersonListDto>>
{
    public async Task<List<PersonListDto>> Handle(GetPeopleQuery request, CancellationToken cancellationToken)
    {
        var query = context.Persons
            .IgnoreQueryFilters()
            .Where(p => p.OrganizationId == request.OrganizationId && !p.IsDeleted);

        if (request.IsActiveFilter.HasValue)
            query = query.Where(p => p.IsActive == request.IsActiveFilter.Value);

        if (request.VoiceGroupIdFilter.HasValue)
            query = query.Where(p => p.VoiceGroupId == request.VoiceGroupIdFilter.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(term) ||
                p.LastName.ToLower().Contains(term));
        }

        var rows = await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => new
            {
                p.Id, p.FirstName, p.LastName, p.Email, p.Phone,
                p.VoiceGroupId,
                VoiceGroupName = p.VoiceGroup != null ? p.VoiceGroup.Name : null,
                p.IsActive,
                IsSuspended = p.IsActive &&
                    context.OrganizationUsers.IgnoreQueryFilters()
                        .Any(ou => ou.PersonId == p.Id && ou.OrganizationId == request.OrganizationId) &&
                    !context.RoleAssignments.IgnoreQueryFilters()
                        .Any(ra => ra.PersonId == p.Id &&
                                   ra.OrganizationId == request.OrganizationId &&
                                   ra.IsActive &&
                                   (ra.ToDate == null || ra.ToDate > DateTime.Today)),
                UserId = context.OrganizationUsers.IgnoreQueryFilters()
                    .Where(ou => ou.PersonId == p.Id && ou.OrganizationId == request.OrganizationId)
                    .Select(ou => ou.UserId)
                    .FirstOrDefault(),
                Roles = context.RoleAssignments.IgnoreQueryFilters()
                    .Where(ra => ra.PersonId == p.Id &&
                                 ra.OrganizationId == request.OrganizationId &&
                                 ra.IsActive &&
                                 ra.FromDate <= DateTime.Today &&
                                 (ra.ToDate == null || ra.ToDate > DateTime.Today))
                    .Select(ra => ra.Role.Name)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var result = new List<PersonListDto>(rows.Count);
        foreach (var row in rows)
        {
            var isPending = row.UserId != null && await userService.IsAccountPendingAsync(row.UserId);
            result.Add(new PersonListDto(
                row.Id, row.FirstName, row.LastName,
                row.Email, row.Phone,
                row.VoiceGroupId, row.VoiceGroupName,
                row.IsActive, row.IsSuspended, isPending, row.Roles));
        }
        return result;
    }
}
