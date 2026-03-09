using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Rehearsals.Commands;

public record UpdateRehearsalTemplateCommand(
    Guid TemplateId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    int DurationMinutes,
    string? Location,
    DateTime StartDate,
    DateTime EndDate) : IRequest<Domain.Common.Result>;

public class UpdateRehearsalTemplateHandler(IAreliaDbContext context)
    : IRequestHandler<UpdateRehearsalTemplateCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        UpdateRehearsalTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await context.RehearsalRecurrenceTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (template is null)
            return Domain.Common.Result.Failure("Rehearsal template not found.");

        var semester = await context.Activities
            .FirstOrDefaultAsync(a => a.Id == template.SemesterId && a.ActivityType == ActivityType.Semester,
                cancellationToken);

        if (semester is null)
            return Domain.Common.Result.Failure("Semester not found.");

        if (request.StartDate < semester.StartDateTime.Date || request.EndDate > semester.EndDateTime.Date)
            return Domain.Common.Result.Failure("Template dates must fall within the semester range.");

        // Check for overlapping templates (excluding self)
        var overlap = await context.RehearsalRecurrenceTemplates
            .AnyAsync(t =>
                t.SemesterId == template.SemesterId &&
                t.Id != request.TemplateId &&
                t.DayOfWeek == request.DayOfWeek &&
                t.StartDate < request.EndDate &&
                t.EndDate > request.StartDate &&
                t.IsActive,
                cancellationToken);

        if (overlap)
            return Domain.Common.Result.Failure("An overlapping template already exists for this day of week.");

        template.DayOfWeek = request.DayOfWeek;
        template.StartTime = request.StartTime;
        template.DurationMinutes = request.DurationMinutes;
        template.Location = request.Location;
        template.StartDate = request.StartDate;
        template.EndDate = request.EndDate;

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
