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
    int ChildActivityCount,
    bool IsImplicitParticipation);

public class GetActivityDetailHandler(IAreliaDbContext context)
    : IRequestHandler<GetActivityDetailQuery, ActivityDetailDto?>
{
    public async Task<ActivityDetailDto?> Handle(
        GetActivityDetailQuery request, CancellationToken cancellationToken)
    {
        var activity = await context.Activities
            .Where(a => a.Id == request.ActivityId)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Description,
                a.ActivityType,
                a.StartDateTime,
                a.EndDateTime,
                a.Location,
                a.WorkYear,
                a.IsPublicVisible,
                a.MaxCapacity,
                a.SignupDeadline,
                a.ParentActivityId,
                ParentName = a.ParentActivity != null ? a.ParentActivity.Name : null,
                ExplicitParticipantCount = a.Participants.Count(p => p.IsActive),
                ChildActivityCount = a.ChildActivities.Count(c => c.IsActive),
                a.IsImplicitParticipation,
                a.OrganizationId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (activity is null)
            return null;

        var participantCount = activity.ExplicitParticipantCount;

        // For implicit-participation activities, count all active members in the organization
        if (activity.IsImplicitParticipation)
        {
            participantCount = await context.Persons
                .Where(p => p.OrganizationId == activity.OrganizationId && p.IsActive)
                .CountAsync(cancellationToken);
        }

        return new ActivityDetailDto(
            activity.Id, activity.Name, activity.Description,
            activity.ActivityType, activity.StartDateTime, activity.EndDateTime,
            activity.Location, activity.WorkYear, activity.IsPublicVisible,
            activity.MaxCapacity, activity.SignupDeadline,
            activity.ParentActivityId, activity.ParentName,
            participantCount, activity.ChildActivityCount,
            activity.IsImplicitParticipation);
    }
}
