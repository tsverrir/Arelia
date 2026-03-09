using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Activities.Queries;

public record GetActivityDetailQuery(Guid ActivityId) : IRequest<ActivityDetailDto?>;

public record ActivityDetailDto(
    Guid Id,
    string Name,
    string? Description,
    ActivityType ActivityType,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string? Location,
    int WorkYear,
    bool IsPublicVisible,
    int? MaxCapacity,
    DateTime? SignupDeadline,
    Guid? ParentActivityId,
    string? ParentName,
    int ParticipantCount,
    int ChildActivityCount);

public class GetActivityDetailHandler(IAreliaDbContext context)
    : IRequestHandler<GetActivityDetailQuery, ActivityDetailDto?>
{
    public async Task<ActivityDetailDto?> Handle(
        GetActivityDetailQuery request, CancellationToken cancellationToken)
    {
        return await context.Activities
            .Where(a => a.Id == request.ActivityId)
            .Select(a => new ActivityDetailDto(
                a.Id, a.Name, a.Description,
                a.ActivityType, a.StartDateTime, a.EndDateTime,
                a.Location, a.WorkYear, a.IsPublicVisible,
                a.MaxCapacity, a.SignupDeadline,
                a.ParentActivityId,
                a.ParentActivity != null ? a.ParentActivity.Name : null,
                a.Participants.Count(p => p.IsActive),
                a.ChildActivities.Count(c => c.IsActive)))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
