using Arelia.Domain.Enums;

namespace Arelia.Application.Reports.Queries;

public record GetAttendanceReportQuery(Guid OrganizationId, Guid? SemesterId = null)
    : IRequest<AttendanceReportDto>;

public record AttendanceReportDto(
    List<AttendancePersonRow> Rows,
    int TotalRehearsals);

public record AttendancePersonRow(
    Guid PersonId,
    string PersonName,
    string? VoiceGroupName,
    int Present,
    int Late,
    int Absent,
    decimal AttendanceRate);

public class GetAttendanceReportHandler(IAreliaDbContext context)
    : IRequestHandler<GetAttendanceReportQuery, AttendanceReportDto>
{
    public async Task<AttendanceReportDto> Handle(
        GetAttendanceReportQuery request, CancellationToken cancellationToken)
    {
        var rehearsalQuery = context.Activities
            .Where(a => a.OrganizationId == request.OrganizationId && a.ActivityType == ActivityType.Rehearsal);

        if (request.SemesterId.HasValue)
            rehearsalQuery = rehearsalQuery.Where(a => a.ParentActivityId == request.SemesterId);

        var rehearsals = await rehearsalQuery.Select(a => a.Id).ToListAsync(cancellationToken);
        var totalRehearsals = rehearsals.Count;

        if (totalRehearsals == 0)
            return new AttendanceReportDto([], 0);

        var records = await context.AttendanceRecords
            .Where(ar => rehearsals.Contains(ar.ActivityId) && ar.IsActive)
            .GroupBy(ar => ar.PersonId)
            .Select(g => new
            {
                PersonId = g.Key,
                Present = g.Count(ar => ar.Status == AttendanceStatus.Present),
                Late = g.Count(ar => ar.Status == AttendanceStatus.Late),
                Absent = g.Count(ar =>
                    ar.Status == AttendanceStatus.NoShow ||
                    ar.Status == AttendanceStatus.Sick ||
                    ar.Status == AttendanceStatus.ExplainedAbsence),
            })
            .ToListAsync(cancellationToken);

        var persons = await context.Persons
            .Where(p => p.OrganizationId == request.OrganizationId)
            .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName, VoiceGroupName = p.VoiceGroup != null ? p.VoiceGroup.Name : (string?)null })
            .ToListAsync(cancellationToken);

        var rows = persons.Select(p =>
        {
            var record = records.FirstOrDefault(r => r.PersonId == p.Id);
            var present = (record?.Present ?? 0) + (record?.Late ?? 0);
            var rate = totalRehearsals > 0 ? (decimal)present / totalRehearsals * 100 : 0;
            return new AttendancePersonRow(
                p.Id, p.Name, p.VoiceGroupName,
                record?.Present ?? 0, record?.Late ?? 0, record?.Absent ?? 0,
                Math.Round(rate, 1));
        })
        .OrderByDescending(r => r.AttendanceRate)
        .ToList();

        return new AttendanceReportDto(rows, totalRehearsals);
    }
}
