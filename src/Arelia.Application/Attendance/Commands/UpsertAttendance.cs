using Arelia.Domain.Entities;
using Arelia.Domain.Enums;

namespace Arelia.Application.Attendance.Commands;

public record UpsertAttendanceCommand(
    Guid ActivityId,
    Guid PersonId,
    AttendanceStatus Status,
    string? Comment) : IRequest<Domain.Common.Result>;

public class UpsertAttendanceHandler(IAreliaDbContext context)
    : IRequestHandler<UpsertAttendanceCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        UpsertAttendanceCommand request, CancellationToken cancellationToken)
    {
        var activity = await context.Activities
            .FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

        if (activity is null)
            return Domain.Common.Result.Failure("Activity not found.");

        var existing = await context.AttendanceRecords
            .FirstOrDefaultAsync(ar =>
                ar.ActivityId == request.ActivityId &&
                ar.PersonId == request.PersonId,
                cancellationToken);

        if (existing is not null)
        {
            existing.Status = request.Status;
            existing.Comment = request.Comment;
            existing.RecordedAt = DateTime.UtcNow;
            existing.IsActive = true;
        }
        else
        {
            context.AttendanceRecords.Add(new AttendanceRecord
            {
                ActivityId = request.ActivityId,
                PersonId = request.PersonId,
                Status = request.Status,
                Comment = request.Comment,
                RecordedAt = DateTime.UtcNow,
                OrganizationId = activity.OrganizationId,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
