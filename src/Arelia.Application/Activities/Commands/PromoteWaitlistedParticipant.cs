using Arelia.Domain.Enums;

namespace Arelia.Application.Activities.Commands;

public record PromoteWaitlistedParticipantCommand(Guid ActivityId, Guid PersonId)
    : IRequest<Domain.Common.Result>;

public class PromoteWaitlistedParticipantHandler(IAreliaDbContext context)
    : IRequestHandler<PromoteWaitlistedParticipantCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        PromoteWaitlistedParticipantCommand request, CancellationToken cancellationToken)
    {
        var activity = await context.Activities
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

        if (activity is null)
            return Domain.Common.Result.Failure("Activity not found.");

        var participant = activity.Participants.FirstOrDefault(p =>
            p.PersonId == request.PersonId &&
            p.IsActive &&
            p.SignupStatus == SignupStatus.Waitlisted);

        if (participant is null)
            return Domain.Common.Result.Failure("Participant is not waitlisted.");

        if (activity.MaxCapacity.HasValue)
        {
            var confirmedCount = activity.Participants.Count(p =>
                p.IsActive &&
                p.SignupStatus == SignupStatus.Confirmed &&
                p.PersonId != request.PersonId);

            if (confirmedCount >= activity.MaxCapacity.Value)
                return Domain.Common.Result.Failure("No confirmed spot is available.");
        }

        participant.SignupStatus = SignupStatus.Confirmed;
        participant.WaitlistPosition = null;
        participant.RsvpStatus = RsvpStatus.Yes;
        participant.RsvpTimestamp = DateTime.UtcNow;

        var position = 1;
        foreach (var waiting in activity.Participants
                     .Where(p => p.IsActive && p.SignupStatus == SignupStatus.Waitlisted)
                     .OrderBy(p => p.WaitlistPosition ?? int.MaxValue)
                     .ThenBy(p => p.RsvpTimestamp))
        {
            waiting.WaitlistPosition = position++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
