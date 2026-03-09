using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    VoiceGroup? VoiceGroup,
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
            // All active members in the organization
            var persons = await context.Persons
                .Where(p => p.OrganizationId == activity.OrganizationId && p.IsActive)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new { p.Id, p.FirstName, p.LastName, p.VoiceGroup })
                .ToListAsync(cancellationToken);

            participants = persons.Select(p =>
            {
                attendanceRecords.TryGetValue(p.Id, out var record);
                return new ParticipantAttendanceDto(
                    p.Id, p.FirstName, p.LastName, p.VoiceGroup,
                    record?.Status, record?.Comment);
            }).ToList();
        }
        else
        {
            // Only explicit participants
            var explicitParticipants = await context.ActivityParticipants
                .Where(ap => ap.ActivityId == request.ActivityId && ap.IsActive)
                .OrderBy(ap => ap.Person.LastName).ThenBy(ap => ap.Person.FirstName)
                .Select(ap => new { ap.PersonId, ap.Person.FirstName, ap.Person.LastName, ap.Person.VoiceGroup })
                .ToListAsync(cancellationToken);

            participants = explicitParticipants.Select(p =>
            {
                attendanceRecords.TryGetValue(p.PersonId, out var record);
                return new ParticipantAttendanceDto(
                    p.PersonId, p.FirstName, p.LastName, p.VoiceGroup,
                    record?.Status, record?.Comment);
            }).ToList();
        }

        return new ActivityAttendanceDto(activity.Id, activity.IsImplicitParticipation, participants);
    }
}
