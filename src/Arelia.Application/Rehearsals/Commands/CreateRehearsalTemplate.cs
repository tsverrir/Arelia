using Arelia.Domain.Entities;
using Arelia.Domain.Enums;

namespace Arelia.Application.Rehearsals.Commands;

public record CreateRehearsalTemplateCommand(
    Guid SemesterId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    int DurationMinutes,
    string? Location,
    DateTime StartDate,
    DateTime EndDate,
    Guid OrganizationId) : IRequest<Domain.Common.Result<Guid>>;

public class CreateRehearsalTemplateHandler(IAreliaDbContext context)
    : IRequestHandler<CreateRehearsalTemplateCommand, Domain.Common.Result<Guid>>
{
    public async Task<Domain.Common.Result<Guid>> Handle(
        CreateRehearsalTemplateCommand request, CancellationToken cancellationToken)
    {
        var semester = await context.Activities
            .FirstOrDefaultAsync(a => a.Id == request.SemesterId && a.ActivityType == ActivityType.Semester,
                cancellationToken);

        if (semester is null)
            return Domain.Common.Result.Failure<Guid>("Semester not found.");

        if (request.StartDate < semester.StartDateTime.Date || request.EndDate > semester.EndDateTime.Date)
            return Domain.Common.Result.Failure<Guid>("Template dates must fall within the semester range.");

        // Check for overlapping templates
        var overlap = await context.RehearsalRecurrenceTemplates
            .AnyAsync(t =>
                t.SemesterId == request.SemesterId &&
                t.DayOfWeek == request.DayOfWeek &&
                t.StartDate < request.EndDate &&
                t.EndDate > request.StartDate &&
                t.IsActive,
                cancellationToken);

        if (overlap)
            return Domain.Common.Result.Failure<Guid>("An overlapping template already exists for this day of week in this semester.");

        var template = new RehearsalRecurrenceTemplate
        {
            SemesterId = request.SemesterId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            DurationMinutes = request.DurationMinutes,
            Location = request.Location,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            OrganizationId = request.OrganizationId,
        };

        context.RehearsalRecurrenceTemplates.Add(template);
        await context.SaveChangesAsync(cancellationToken);

        return Domain.Common.Result.Success(template.Id);
    }
}
