using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Semesters.Queries;

public record GetSemesterDetailQuery(Guid SemesterId) : IRequest<SemesterDetailDto?>;

public record SemesterDetailDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string? Location,
    int WorkYear,
    bool IsPublicVisible,
    int ParticipantCount,
    List<SemesterChildActivityDto> ChildActivities,
    List<RehearsalTemplateDto> RehearsalTemplates);

public record SemesterChildActivityDto(
    Guid Id,
    string Name,
    ActivityType ActivityType,
    ActivityStatus Status,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string? Location,
    bool IsImplicitParticipation,
    int ParticipantCount);

public record RehearsalTemplateDto(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    int DurationMinutes,
    string? Location,
    DateTime StartDate,
    DateTime EndDate);

public class GetSemesterDetailHandler(IAreliaDbContext context)
    : IRequestHandler<GetSemesterDetailQuery, SemesterDetailDto?>
{
    public async Task<SemesterDetailDto?> Handle(
        GetSemesterDetailQuery request, CancellationToken cancellationToken)
    {
        var semester = await context.Activities
            .Where(a => a.Id == request.SemesterId && a.ActivityType == ActivityType.Semester)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Description,
                a.StartDateTime,
                a.EndDateTime,
                a.Location,
                a.WorkYear,
                a.IsPublicVisible,
                a.OrganizationId,
                ParticipantCount = a.Participants.Count(p => p.IsActive),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (semester is null)
            return null;

        // Count active members with 'Member' role for implicit-participation activities
        var now = DateTime.UtcNow;
        var activeMemberCount = await context.Persons
            .Where(p => p.OrganizationId == semester.OrganizationId && p.IsActive
                && context.RoleAssignments.Any(ra =>
                    ra.PersonId == p.Id && ra.IsActive
                    && ra.Role.Name == "Member"
                    && ra.FromDate <= now
                    && (ra.ToDate == null || ra.ToDate >= now)))
            .CountAsync(cancellationToken);

        var childActivities = await context.Activities
            .Where(a => a.ParentActivityId == request.SemesterId)
            .OrderBy(a => a.StartDateTime)
            .Select(a => new SemesterChildActivityDto(
                a.Id,
                a.Name,
                a.ActivityType,
                a.Status,
                a.StartDateTime,
                a.EndDateTime,
                a.Location,
                a.IsImplicitParticipation,
                a.IsImplicitParticipation ? activeMemberCount : a.Participants.Count(p => p.IsActive)))
            .ToListAsync(cancellationToken);

        var templates = await context.RehearsalRecurrenceTemplates
            .Where(t => t.SemesterId == request.SemesterId && t.IsActive)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.StartTime)
            .Select(t => new RehearsalTemplateDto(
                t.Id,
                t.DayOfWeek,
                t.StartTime,
                t.DurationMinutes,
                t.Location,
                t.StartDate,
                t.EndDate))
            .ToListAsync(cancellationToken);

        return new SemesterDetailDto(
            semester.Id,
            semester.Name,
            semester.Description,
            semester.StartDateTime,
            semester.EndDateTime,
            semester.Location,
            semester.WorkYear,
            semester.IsPublicVisible,
            semester.ParticipantCount,
            childActivities,
            templates);
    }
}
