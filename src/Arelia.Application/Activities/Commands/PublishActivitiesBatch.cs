using Arelia.Domain.Enums;

namespace Arelia.Application.Activities.Commands;

public record PublishActivitiesBatchCommand(List<Guid> ActivityIds) : IRequest<Domain.Common.Result<int>>;

public class PublishActivitiesBatchHandler(IAreliaDbContext context)
    : IRequestHandler<PublishActivitiesBatchCommand, Domain.Common.Result<int>>
{
    public async Task<Domain.Common.Result<int>> Handle(
        PublishActivitiesBatchCommand request, CancellationToken cancellationToken)
    {
        if (request.ActivityIds.Count == 0)
            return Domain.Common.Result.Failure<int>("No activities selected.");

        var activities = await context.Activities
            .Where(a => request.ActivityIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var draftActivities = activities
            .Where(a => a.Status == ActivityStatus.Draft)
            .ToList();

        if (draftActivities.Count == 0)
            return Domain.Common.Result.Failure<int>("No draft activities found in the selection.");

        foreach (var activity in draftActivities)
        {
            activity.Status = ActivityStatus.Published;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success<int>(draftActivities.Count);
    }
}
