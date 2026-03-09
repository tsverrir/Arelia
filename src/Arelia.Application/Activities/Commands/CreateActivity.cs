using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Activities.Commands;

public record CreateActivityCommand(
    string Name,
    string? Description,
    ActivityType ActivityType,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string? Location,
    Guid? ParentActivityId,
    bool IsPublicVisible,
    int? MaxCapacity,
    DateTime? SignupDeadline,
    Guid OrganizationId) : IRequest<Domain.Common.Result<Guid>>;

public class CreateActivityHandler(IAreliaDbContext context)
    : IRequestHandler<CreateActivityCommand, Domain.Common.Result<Guid>>
{
    public async Task<Domain.Common.Result<Guid>> Handle(
        CreateActivityCommand request, CancellationToken cancellationToken)
    {
        // Validate nesting: max one level
        if (request.ParentActivityId.HasValue)
        {
            var parent = await context.Activities
                .FirstOrDefaultAsync(a => a.Id == request.ParentActivityId.Value, cancellationToken);

            if (parent is null)
                return Domain.Common.Result.Failure<Guid>("Parent activity not found.");

            if (parent.ParentActivityId.HasValue)
                return Domain.Common.Result.Failure<Guid>("Activities can only be nested one level deep.");

            if (parent.ActivityType != ActivityType.Semester)
                return Domain.Common.Result.Failure<Guid>("Activities can only be nested under semesters.");
        }

        // Validate semester non-overlap
        if (request.ActivityType == ActivityType.Semester)
        {
            var overlap = await context.Activities
                .Where(a =>
                    a.OrganizationId == request.OrganizationId &&
                    a.ActivityType == ActivityType.Semester &&
                    a.StartDateTime < request.EndDateTime &&
                    a.EndDateTime > request.StartDateTime)
                .AnyAsync(cancellationToken);

            if (overlap)
                return Domain.Common.Result.Failure<Guid>("Semester dates overlap with an existing semester.");
        }

        var activity = new Activity
        {
            Name = request.Name,
            Description = request.Description,
            ActivityType = request.ActivityType,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Location = request.Location,
            ParentActivityId = request.ParentActivityId,
            WorkYear = request.StartDateTime.Year,
            IsPublicVisible = request.IsPublicVisible,
            MaxCapacity = request.MaxCapacity,
            SignupDeadline = request.SignupDeadline,
            OrganizationId = request.OrganizationId,
        };

        context.Activities.Add(activity);
        await context.SaveChangesAsync(cancellationToken);

        return Domain.Common.Result.Success(activity.Id);
    }
}
