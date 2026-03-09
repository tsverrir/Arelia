using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Activities.Commands;

public record BulkDeleteActivitiesCommand(List<Guid> ActivityIds) : IRequest<Domain.Common.Result>;

public class BulkDeleteActivitiesHandler(IAreliaDbContext context)
    : IRequestHandler<BulkDeleteActivitiesCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        BulkDeleteActivitiesCommand request, CancellationToken cancellationToken)
    {
        if (request.ActivityIds.Count == 0)
            return Domain.Common.Result.Failure("No activities specified.");

        var activities = await context.Activities
            .Include(a => a.ChildActivities)
            .Where(a => request.ActivityIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        foreach (var activity in activities)
        {
            foreach (var child in activity.ChildActivities)
            {
                child.IsActive = false;
            }

            activity.IsActive = false;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
