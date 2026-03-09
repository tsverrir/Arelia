using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    DateTime? SignupDeadline) : IRequest<Domain.Common.Result>;

public class UpdateActivityHandler(IAreliaDbContext context)
    : IRequestHandler<UpdateActivityCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        UpdateActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = await context.Activities
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (activity is null)
            return Domain.Common.Result.Failure("Activity not found.");

        // Validate semester date constraints
        if (activity.ActivityType == ActivityType.Semester &&
            request.StartDateTime.Year != request.EndDateTime.Year)
        {
            return Domain.Common.Result.Failure("Semesters cannot cross calendar year boundaries.");
        }

        activity.Name = request.Name;
        activity.Description = request.Description;
        activity.StartDateTime = request.StartDateTime;
        activity.EndDateTime = request.EndDateTime;
        activity.Location = request.Location;
        activity.IsPublicVisible = request.IsPublicVisible;
        activity.MaxCapacity = request.MaxCapacity;
        activity.SignupDeadline = request.SignupDeadline;
        activity.WorkYear = request.StartDateTime.Year;

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
