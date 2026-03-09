using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.People.Queries;

public record GetPeopleQuery(
    Guid OrganizationId,
    VoiceGroup? VoiceGroupFilter = null,
    bool? IsActiveFilter = null,
    string? SearchTerm = null) : IRequest<List<PersonListDto>>;

public record PersonListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    VoiceGroup? VoiceGroup,
    bool IsActive);

public class GetPeopleHandler(IAreliaDbContext context)
    : IRequestHandler<GetPeopleQuery, List<PersonListDto>>
{
    public async Task<List<PersonListDto>> Handle(GetPeopleQuery request, CancellationToken cancellationToken)
    {
        var query = context.Persons
            .IgnoreQueryFilters()
            .Where(p => p.OrganizationId == request.OrganizationId);

        if (request.IsActiveFilter.HasValue)
            query = query.Where(p => p.IsActive == request.IsActiveFilter.Value);
        else
            query = query.Where(p => p.IsActive);

        if (request.VoiceGroupFilter.HasValue)
            query = query.Where(p => p.VoiceGroup == request.VoiceGroupFilter.Value);

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
                p.Email, p.Phone, p.VoiceGroup, p.IsActive))
            .ToListAsync(cancellationToken);
    }
}
