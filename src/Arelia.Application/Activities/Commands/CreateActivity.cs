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
    Guid OrganizationId,
    bool RsvpEnabled = false,
    bool WaitingListEnabled = true,
    ActivityStatus Status = ActivityStatus.Draft) : IRequest<Domain.Common.Result<Guid>>;

public class CreateActivityHandler(IAreliaDbContext context)
    : IRequestHandler<CreateActivityCommand, Domain.Common.Result<Guid>>
{
    public async Task<Domain.Common.Result<Guid>> Handle(
        CreateActivityCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Domain.Common.Result.Failure<Guid>("Activity name is required.");

        if (request.EndDateTime < request.StartDateTime)
            return Domain.Common.Result.Failure<Guid>("End date/time must be after start date/time.");

        if (request.MaxCapacity is <= 0)
            return Domain.Common.Result.Failure<Guid>("Capacity must be a positive number.");

        if (request.SignupDeadline.HasValue && request.SignupDeadline.Value > request.EndDateTime)
            return Domain.Common.Result.Failure<Guid>("Signup deadline must be before the activity ends.");

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

        // Validate semester dates within a single calendar year
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

            if (request.StartDateTime.Year != request.EndDateTime.Year)
                return Domain.Common.Result.Failure<Guid>("Semesters cannot cross calendar year boundaries.");
        }

        var activity = new Activity
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            ActivityType = request.ActivityType,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Location = request.Location,
            ParentActivityId = request.ParentActivityId,
            WorkYear = request.StartDateTime.Year,
            IsPublicVisible = request.IsPublicVisible,
            Status = request.Status,
            RsvpEnabled = request.RsvpEnabled,
            IsImplicitParticipation = request.ActivityType == ActivityType.Rehearsal,
            MaxCapacity = request.MaxCapacity,
            SignupDeadline = request.SignupDeadline,
            WaitingListEnabled = request.WaitingListEnabled,
            OrganizationId = request.OrganizationId,
        };

        context.Activities.Add(activity);
        await context.SaveChangesAsync(cancellationToken);

        return Domain.Common.Result.Success(activity.Id);
    }
}
