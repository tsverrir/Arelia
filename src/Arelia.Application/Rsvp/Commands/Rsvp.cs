using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Rsvp.Commands;

public record RsvpCommand(
    Guid ActivityId,
    Guid PersonId,
    RsvpStatus Status,
    Guid OrganizationId) : IRequest<Domain.Common.Result>;

public class RsvpHandler(IAreliaDbContext context)
    : IRequestHandler<RsvpCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(RsvpCommand request, CancellationToken cancellationToken)
    {
        var activity = await context.Activities
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a =>
                a.Id == request.ActivityId &&
                a.OrganizationId == request.OrganizationId,
                cancellationToken);

        if (activity is null)
            return Domain.Common.Result.Failure("Activity not found.");

        var isRehearsal = activity.ActivityType == ActivityType.Rehearsal;

        // Deadline check (non-rehearsals only, board can override via separate mechanism)
        if (!isRehearsal && activity.SignupDeadline.HasValue && DateTime.UtcNow > activity.SignupDeadline.Value)
            return Domain.Common.Result.Failure("The signup deadline has passed.");

        var participant = activity.Participants
            .FirstOrDefault(p => p.PersonId == request.PersonId && p.IsActive);

        if (isRehearsal)
        {
            // Absence model: only create record when No or Maybe
            var result = HandleRehearsalRsvp(activity, participant, request, cancellationToken);
            if (result.IsFailure)
                return result;

            await context.SaveChangesAsync(cancellationToken);
            return result;
        }

        return await HandleOptInRsvp(activity, participant, request, cancellationToken);
    }

    private Domain.Common.Result HandleRehearsalRsvp(
        Activity activity, ActivityParticipant? participant,
        RsvpCommand request, CancellationToken cancellationToken)
    {
        if (request.Status == RsvpStatus.Yes || request.Status == RsvpStatus.Unanswered)
        {
            // Remove absence record
            if (participant is not null)
                participant.IsActive = false;
        }
        else
        {
            // Create/update absence record
            if (participant is null)
            {
                context.ActivityParticipants.Add(new ActivityParticipant
                {
                    ActivityId = activity.Id,
                    PersonId = request.PersonId,
                    RsvpStatus = request.Status,
                    RsvpTimestamp = DateTime.UtcNow,
                    OrganizationId = request.OrganizationId,
                });
            }
            else
            {
                participant.RsvpStatus = request.Status;
                participant.RsvpTimestamp = DateTime.UtcNow;
            }
        }

        return Domain.Common.Result.Success();
    }

    private async Task<Domain.Common.Result> HandleOptInRsvp(
        Activity activity, ActivityParticipant? participant,
        RsvpCommand request, CancellationToken cancellationToken)
    {
        if (participant is null)
        {
            participant = new ActivityParticipant
            {
                ActivityId = activity.Id,
                PersonId = request.PersonId,
                OrganizationId = request.OrganizationId,
            };
            context.ActivityParticipants.Add(participant);
        }

        participant.RsvpStatus = request.Status;
        participant.RsvpTimestamp = DateTime.UtcNow;

        // Capacity / waitlist logic
        if (activity.MaxCapacity.HasValue && request.Status == RsvpStatus.Yes)
        {
            var confirmedCount = activity.Participants
                .Count(p => p.IsActive && p.SignupStatus == SignupStatus.Confirmed && p.PersonId != request.PersonId);

            if (confirmedCount >= activity.MaxCapacity.Value)
            {
                if (!activity.WaitingListEnabled)
                    return Domain.Common.Result.Failure("The activity is full and the waiting list is disabled.");

                // Waitlist
                var maxPos = activity.Participants
                    .Where(p => p.IsActive && p.SignupStatus == SignupStatus.Waitlisted)
                    .Select(p => p.WaitlistPosition ?? 0)
                    .DefaultIfEmpty(0)
                    .Max();

                participant.SignupStatus = SignupStatus.Waitlisted;
                participant.WaitlistPosition = maxPos + 1;
            }
            else
            {
                participant.SignupStatus = SignupStatus.Confirmed;
                participant.WaitlistPosition = null;
            }
        }
        else if (request.Status == RsvpStatus.Yes)
        {
            participant.SignupStatus = SignupStatus.Confirmed;
        }
        else
        {
            // Release spot
            participant.SignupStatus = SignupStatus.None;
            participant.WaitlistPosition = null;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
