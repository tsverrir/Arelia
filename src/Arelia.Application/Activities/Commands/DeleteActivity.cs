
namespace Arelia.Application.Activities.Commands;

public record DeleteActivityCommand(Guid ActivityId) : IRequest<Domain.Common.Result>;

public class DeleteActivityHandler(IAreliaDbContext context)
    : IRequestHandler<DeleteActivityCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        DeleteActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = await context.Activities
            .Include(a => a.ChildActivities)
            .FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

        if (activity is null)
            return Domain.Common.Result.Failure("Activity not found.");

        // Soft-delete child activities
        foreach (var child in activity.ChildActivities)
        {
            child.IsActive = false;
        }

        activity.IsActive = false;

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
