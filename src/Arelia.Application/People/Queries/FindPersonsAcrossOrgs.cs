using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.People.Queries;

/// <summary>
/// Searches for persons by name across all organizations, excluding the given org.
/// Used to detect cross-org duplicates and to import persons from other orgs.
/// </summary>
public record FindPersonsAcrossOrgsQuery(
    string SearchTerm,
    Guid ExcludeOrganizationId) : IRequest<List<PersonAcrossOrgDto>>;

public record PersonAcrossOrgDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    Guid? VoiceGroupId,
    Guid OrganizationId,
    string OrganizationName);

public class FindPersonsAcrossOrgsHandler(IAreliaDbContext context)
    : IRequestHandler<FindPersonsAcrossOrgsQuery, List<PersonAcrossOrgDto>>
{
    public async Task<List<PersonAcrossOrgDto>> Handle(
        FindPersonsAcrossOrgsQuery request, CancellationToken cancellationToken)
    {
        var term = request.SearchTerm.ToLower();

        return await (
            from p in context.Persons.IgnoreQueryFilters()
            join o in context.Organizations on p.OrganizationId equals o.Id
            where p.IsActive && o.IsActive
            where p.OrganizationId != request.ExcludeOrganizationId
            where p.FirstName.ToLower().Contains(term) || p.LastName.ToLower().Contains(term)
            orderby p.LastName, p.FirstName
            select new PersonAcrossOrgDto(
                p.Id, p.FirstName, p.LastName, p.Email, p.Phone, p.VoiceGroupId,
                o.Id, o.Name)
        ).ToListAsync(cancellationToken);
    }
}
