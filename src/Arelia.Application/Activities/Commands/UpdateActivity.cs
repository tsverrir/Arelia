using Arelia.Domain.Enums;

namespace Arelia.Application.Activities.Commands;

public record UpdateActivityCommand(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string? Location,
    bool IsPublicVisible,
    int? MaxCapacity,
    DateTime? SignupDeadline,
    ActivityType? ActivityType = null,
    ActivityStatus? Status = null,
    bool? RsvpEnabled = null,
    bool? WaitingListEnabled = null) : IRequest<Domain.Common.Result>;

public class UpdateActivityHandler(IAreliaDbContext context)
    : IRequestHandler<UpdateActivityCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        UpdateActivityCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Domain.Common.Result.Failure("Activity name is required.");

        if (request.EndDateTime < request.StartDateTime)
            return Domain.Common.Result.Failure("End date/time must be after start date/time.");

        if (request.MaxCapacity is <= 0)
            return Domain.Common.Result.Failure("Capacity must be a positive number.");

        if (request.SignupDeadline.HasValue && request.SignupDeadline.Value > request.EndDateTime)
            return Domain.Common.Result.Failure("Signup deadline must be before the activity ends.");

        var activity = await context.Activities
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (activity is null)
            return Domain.Common.Result.Failure("Activity not found.");

        // Validate semester date constraints
        var effectiveType = request.ActivityType ?? activity.ActivityType;
        if (effectiveType == ActivityType.Semester &&
            request.StartDateTime.Year != request.EndDateTime.Year)
        {
            return Domain.Common.Result.Failure("Semesters cannot cross calendar year boundaries.");
        }

        activity.Name = request.Name.Trim();
        activity.Description = request.Description;
        activity.StartDateTime = request.StartDateTime;
        activity.EndDateTime = request.EndDateTime;
        activity.Location = request.Location;
        activity.IsPublicVisible = request.IsPublicVisible;
        activity.MaxCapacity = request.MaxCapacity;
        activity.SignupDeadline = request.SignupDeadline;
        activity.WorkYear = request.StartDateTime.Year;
        activity.Status = request.Status ?? activity.Status;
        activity.RsvpEnabled = request.RsvpEnabled ?? activity.RsvpEnabled;
        activity.WaitingListEnabled = request.WaitingListEnabled ?? activity.WaitingListEnabled;

        if (request.ActivityType.HasValue)
            activity.ActivityType = request.ActivityType.Value;

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
