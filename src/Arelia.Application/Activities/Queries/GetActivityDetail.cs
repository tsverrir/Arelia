using Arelia.Domain.Enums;

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
    ActivityStatus Status,
    bool RsvpEnabled,
    int? MaxCapacity,
    DateTime? SignupDeadline,
    bool WaitingListEnabled,
    Guid? ParentActivityId,
    string? ParentName,
    int ParticipantCount,
    int ChildActivityCount,
    bool IsImplicitParticipation,
    int ConfirmedCount,
    int RsvpYesCount,
    int RsvpNoCount,
    int RsvpMaybeCount,
    int WaitlistCount);

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
                a.Status,
                a.RsvpEnabled,
                a.MaxCapacity,
                a.SignupDeadline,
                a.WaitingListEnabled,
                a.ParentActivityId,
                ParentName = a.ParentActivity != null ? a.ParentActivity.Name : null,
                ExplicitParticipantCount = a.Participants.Count(p => p.IsActive),
                ChildActivityCount = a.ChildActivities.Count(c => c.IsActive),
                a.IsImplicitParticipation,
                RsvpYesCount = a.Participants.Count(p => p.IsActive && p.RsvpStatus == RsvpStatus.Yes),
                RsvpNoCount = a.Participants.Count(p => p.IsActive && p.RsvpStatus == RsvpStatus.No),
                RsvpMaybeCount = a.Participants.Count(p => p.IsActive && p.RsvpStatus == RsvpStatus.Maybe),
                ConfirmedCount = a.Participants.Count(p => p.IsActive && p.SignupStatus == SignupStatus.Confirmed),
                WaitlistCount = a.Participants.Count(p => p.IsActive && p.SignupStatus == SignupStatus.Waitlisted),
                a.OrganizationId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (activity is null)
            return null;

        var participantCount = activity.ExplicitParticipantCount;

        // For implicit-participation activities, count persons with active 'Member' role
        if (activity.IsImplicitParticipation)
        {
            participantCount = await context.Persons
                .Where(p => p.OrganizationId == activity.OrganizationId && p.IsActive
                    && context.RoleAssignments.Any(ra =>
                        ra.PersonId == p.Id && ra.IsActive
                        && ra.Role.Name == "Member"
                        && ra.FromDate <= activity.StartDateTime
                        && (ra.ToDate == null || ra.ToDate >= activity.StartDateTime)))
                .CountAsync(cancellationToken);
        }

        return new ActivityDetailDto(
            activity.Id, activity.Name, activity.Description,
            activity.ActivityType, activity.StartDateTime, activity.EndDateTime,
            activity.Location, activity.WorkYear, activity.IsPublicVisible,
            activity.Status, activity.RsvpEnabled,
            activity.MaxCapacity, activity.SignupDeadline, activity.WaitingListEnabled,
            activity.ParentActivityId, activity.ParentName,
            participantCount, activity.ChildActivityCount,
            activity.IsImplicitParticipation,
            activity.ConfirmedCount,
            activity.RsvpYesCount, activity.RsvpNoCount, activity.RsvpMaybeCount, activity.WaitlistCount);
    }
}
