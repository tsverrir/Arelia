using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Rehearsals.Commands;

public record GenerateRehearsalsCommand(Guid SemesterId, Guid OrganizationId)
    : IRequest<Domain.Common.Result<GenerateRehearsalsResult>>;

public record GenerateRehearsalsResult(int Created, int Skipped, List<string> Conflicts);

public class GenerateRehearsalsHandler(IAreliaDbContext context)
    : IRequestHandler<GenerateRehearsalsCommand, Domain.Common.Result<GenerateRehearsalsResult>>
{
    public async Task<Domain.Common.Result<GenerateRehearsalsResult>> Handle(
        GenerateRehearsalsCommand request, CancellationToken cancellationToken)
    {
        var templates = await context.RehearsalRecurrenceTemplates
            .Where(t => t.SemesterId == request.SemesterId)
            .ToListAsync(cancellationToken);

        if (templates.Count == 0)
            return Domain.Common.Result.Failure<GenerateRehearsalsResult>("No templates found for this semester.");

        var existingRehearsals = await context.Activities
            .Where(a =>
                a.OrganizationId == request.OrganizationId &&
                a.ActivityType == ActivityType.Rehearsal)
            .Select(a => new { a.StartDateTime })
            .ToListAsync(cancellationToken);

        var existingStartTimes = existingRehearsals
            .Select(r => r.StartDateTime)
            .ToHashSet();

        // Get all non-rehearsal activities in the range for conflict detection
        var allActivities = await context.Activities
            .Where(a =>
                a.OrganizationId == request.OrganizationId &&
                a.ActivityType != ActivityType.Rehearsal &&
                a.ActivityType != ActivityType.Semester)
            .Select(a => new { a.Name, a.StartDateTime, a.EndDateTime })
            .ToListAsync(cancellationToken);

        int created = 0;
        int skipped = 0;
        var conflicts = new List<string>();

        foreach (var template in templates)
        {
            var current = template.StartDate;
            while (current <= template.EndDate)
            {
                if (current.DayOfWeek == template.DayOfWeek)
                {
                    var start = current.Date + template.StartTime.ToTimeSpan();
                    var end = start.AddMinutes(template.DurationMinutes);

                    // Idempotent: skip if rehearsal already exists at this time
                    if (existingStartTimes.Contains(start))
                    {
                        skipped++;
                        current = current.AddDays(1);
                        continue;
                    }

                    // Check for conflicts with other activities
                    var conflict = allActivities
                        .FirstOrDefault(a => a.StartDateTime < end && a.EndDateTime > start);

                    if (conflict is not null)
                    {
                        conflicts.Add($"{current:yyyy-MM-dd}: Conflicts with '{conflict.Name}'");
                        current = current.AddDays(1);
                        continue;
                    }

                    context.Activities.Add(new Activity
                    {
                        Name = $"Rehearsal",
                        ActivityType = ActivityType.Rehearsal,
                        StartDateTime = start,
                        EndDateTime = end,
                        Location = template.Location,
                        ParentActivityId = request.SemesterId,
                        WorkYear = start.Year,
                        IsImplicitParticipation = true,
                        OrganizationId = request.OrganizationId,
                    });

                    existingStartTimes.Add(start);
                    created++;
                }
                current = current.AddDays(1);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return Domain.Common.Result.Success(new GenerateRehearsalsResult(created, skipped, conflicts));
    }
}
