
namespace Arelia.Application.VoiceGroups.Queries;

public record GetVoiceGroupsQuery(Guid OrganizationId) : IRequest<List<VoiceGroupDto>>;

public record VoiceGroupDto(Guid Id, string Name, int SortOrder, int PeopleCount);

public class GetVoiceGroupsHandler(IAreliaDbContext context)
    : IRequestHandler<GetVoiceGroupsQuery, List<VoiceGroupDto>>
{
    public async Task<List<VoiceGroupDto>> Handle(GetVoiceGroupsQuery request, CancellationToken cancellationToken)
    {
        return await context.VoiceGroups
            .IgnoreQueryFilters()
            .Where(v => v.OrganizationId == request.OrganizationId && v.IsActive)
            .OrderBy(v => v.SortOrder).ThenBy(v => v.Name)
            .Select(v => new VoiceGroupDto(
                v.Id,
                v.Name,
                v.SortOrder,
                v.People.Count(p => p.IsActive && !p.IsDeleted)))
            .ToListAsync(cancellationToken);
    }
}
