using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Activities.Commands;

public record CancelActivitiesBatchCommand(List<Guid> ActivityIds) : IRequest<Domain.Common.Result<int>>;

public class CancelActivitiesBatchHandler(IAreliaDbContext context)
    : IRequestHandler<CancelActivitiesBatchCommand, Domain.Common.Result<int>>
{
    public async Task<Domain.Common.Result<int>> Handle(
        CancelActivitiesBatchCommand request, CancellationToken cancellationToken)
    {
        if (request.ActivityIds.Count == 0)
            return Domain.Common.Result.Failure<int>("No activities selected.");

        var activities = await context.Activities
            .Where(a => request.ActivityIds.Contains(a.Id))
            .Where(a => a.Status != ActivityStatus.Cancelled)
            .ToListAsync(cancellationToken);

        if (activities.Count == 0)
            return Domain.Common.Result.Failure<int>("No cancellable activities found in the selection.");

        foreach (var activity in activities)
        {
            activity.Status = ActivityStatus.Cancelled;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success(activities.Count);
    }
}
