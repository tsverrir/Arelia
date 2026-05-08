using Arelia.Domain.Enums;

namespace Arelia.Application.Attendance.Queries;

public record GetActivityAttendanceQuery(Guid ActivityId) : IRequest<ActivityAttendanceDto?>;

public record ActivityAttendanceDto(
    Guid ActivityId,
    bool IsImplicitParticipation,
    List<ParticipantAttendanceDto> Participants);

public record ParticipantAttendanceDto(
    Guid PersonId,
    string FirstName,
    string LastName,
    string? VoiceGroupName,
    AttendanceStatus? Status,
    string? Comment);

public class GetActivityAttendanceHandler(IAreliaDbContext context)
    : IRequestHandler<GetActivityAttendanceQuery, ActivityAttendanceDto?>
{
    public async Task<ActivityAttendanceDto?> Handle(
        GetActivityAttendanceQuery request, CancellationToken cancellationToken)
    {
        var activity = await context.Activities
            .Where(a => a.Id == request.ActivityId)
            .Select(a => new { a.Id, a.OrganizationId, a.IsImplicitParticipation })
            .FirstOrDefaultAsync(cancellationToken);

        if (activity is null)
            return null;

        // Get existing attendance records for this activity
        var attendanceRecords = await context.AttendanceRecords
            .Where(ar => ar.ActivityId == request.ActivityId && ar.IsActive)
            .ToDictionaryAsync(ar => ar.PersonId, ar => new { ar.Status, ar.Comment }, cancellationToken);

        List<ParticipantAttendanceDto> participants;

        if (activity.IsImplicitParticipation)
        {
            // Only persons with an active 'Member' role assignment
            var now = DateTime.UtcNow;
            var persons = await context.Persons
                .Where(p => p.OrganizationId == activity.OrganizationId && p.IsActive
                    && context.RoleAssignments.Any(ra =>
                        ra.PersonId == p.Id && ra.IsActive
                        && ra.Role.Name == "Member"
                        && ra.FromDate <= now
                        && (ra.ToDate == null || ra.ToDate >= now)))
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new { p.Id, p.FirstName, p.LastName, VoiceGroupName = p.VoiceGroup != null ? p.VoiceGroup.Name : (string?)null })
                .ToListAsync(cancellationToken);

            participants = persons.Select(p =>
            {
                attendanceRecords.TryGetValue(p.Id, out var record);
                return new ParticipantAttendanceDto(
                    p.Id, p.FirstName, p.LastName, p.VoiceGroupName,
                    record?.Status, record?.Comment);
            }).ToList();
        }
        else
        {
            // Only explicit participants
            var explicitParticipants = await context.ActivityParticipants
                .Where(ap => ap.ActivityId == request.ActivityId && ap.IsActive)
                .OrderBy(ap => ap.Person.LastName).ThenBy(ap => ap.Person.FirstName)
                .Select(ap => new { ap.PersonId, ap.Person.FirstName, ap.Person.LastName, VoiceGroupName = ap.Person.VoiceGroup != null ? ap.Person.VoiceGroup.Name : (string?)null })
                .ToListAsync(cancellationToken);

            participants = explicitParticipants.Select(p =>
            {
                attendanceRecords.TryGetValue(p.PersonId, out var record);
                return new ParticipantAttendanceDto(
                    p.PersonId, p.FirstName, p.LastName, p.VoiceGroupName,
                    record?.Status, record?.Comment);
            }).ToList();
        }

        return new ActivityAttendanceDto(activity.Id, activity.IsImplicitParticipation, participants);
    }
}
