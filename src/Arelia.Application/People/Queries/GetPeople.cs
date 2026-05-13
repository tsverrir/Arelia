
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
    bool IsSuspended);

public class GetPeopleHandler(IAreliaDbContext context)
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

        return await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => new PersonListDto(
                p.Id, p.FirstName, p.LastName,
                p.Email, p.Phone,
                p.VoiceGroupId, p.VoiceGroup != null ? p.VoiceGroup.Name : null,
                p.IsActive,
                p.IsActive &&
                    context.OrganizationUsers.IgnoreQueryFilters()
                        .Any(ou => ou.PersonId == p.Id && ou.OrganizationId == request.OrganizationId) &&
                    !context.RoleAssignments.IgnoreQueryFilters()
                        .Any(ra => ra.PersonId == p.Id &&
                                   ra.OrganizationId == request.OrganizationId &&
                                   ra.IsActive &&
                                   (ra.ToDate == null || ra.ToDate > DateTime.UtcNow))))
            .ToListAsync(cancellationToken);
    }
}
