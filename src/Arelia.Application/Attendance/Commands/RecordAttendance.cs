using Arelia.Domain.Entities;
using Arelia.Domain.Enums;

namespace Arelia.Application.Attendance.Commands;

public record RecordAttendanceCommand(
    Guid ActivityId,
    Guid PersonId,
    AttendanceStatus Status,
    string RecordedByUserId,
    Guid OrganizationId) : IRequest<Domain.Common.Result>;

public class RecordAttendanceHandler(IAreliaDbContext context)
    : IRequestHandler<RecordAttendanceCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        RecordAttendanceCommand request, CancellationToken cancellationToken)
    {
        var existing = await context.AttendanceRecords
            .FirstOrDefaultAsync(ar =>
                ar.ActivityId == request.ActivityId && ar.PersonId == request.PersonId,
                cancellationToken);

        if (existing is not null)
        {
            existing.Status = request.Status;
            existing.RecordedByUserId = request.RecordedByUserId;
            existing.RecordedAt = DateTime.UtcNow;
        }
        else
        {
            context.AttendanceRecords.Add(new AttendanceRecord
            {
                ActivityId = request.ActivityId,
                PersonId = request.PersonId,
                Status = request.Status,
                RecordedByUserId = request.RecordedByUserId,
                RecordedAt = DateTime.UtcNow,
                OrganizationId = request.OrganizationId,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
