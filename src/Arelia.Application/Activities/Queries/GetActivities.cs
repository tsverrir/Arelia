using Arelia.Domain.Enums;

namespace Arelia.Application.Activities.Queries;

public record GetActivitiesQuery(
    Guid OrganizationId,
    int? WorkYear = null,
    ActivityType? TypeFilter = null) : IRequest<List<ActivityListDto>>;

public record ActivityListDto(
    Guid Id,
    string Name,
    ActivityType ActivityType,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string? Location,
    int WorkYear,
    bool IsPublicVisible,
    Guid? ParentActivityId,
    string? ParentName);

public class GetActivitiesHandler(IAreliaDbContext context)
    : IRequestHandler<GetActivitiesQuery, List<ActivityListDto>>
{
    public async Task<List<ActivityListDto>> Handle(
        GetActivitiesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Activities.Where(a => a.OrganizationId == request.OrganizationId);

        if (request.WorkYear.HasValue)
            query = query.Where(a => a.WorkYear == request.WorkYear.Value);

        if (request.TypeFilter.HasValue)
            query = query.Where(a => a.ActivityType == request.TypeFilter.Value);

        return await query
            .OrderBy(a => a.StartDateTime)
            .Select(a => new ActivityListDto(
                a.Id, a.Name, a.ActivityType,
                a.StartDateTime, a.EndDateTime,
                a.Location, a.WorkYear, a.IsPublicVisible,
                a.ParentActivityId,
                a.ParentActivity != null ? a.ParentActivity.Name : null))
            .ToListAsync(cancellationToken);
    }
}
