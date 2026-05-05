using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Activities.Queries;

public record ListActivityParticipantsQuery(Guid ActivityId) : IRequest<List<ActivityParticipantDto>>;

public record ActivityParticipantDto(
    Guid ParticipantId,
    Guid PersonId,
    string PersonName,
    string? VoiceGroupName,
    ParticipationExpectationStatus ExpectationStatus,
    RsvpStatus RsvpStatus,
    SignupStatus SignupStatus,
    int? WaitlistPosition,
    string? Note);

public class ListActivityParticipantsHandler(IAreliaDbContext context)
    : IRequestHandler<ListActivityParticipantsQuery, List<ActivityParticipantDto>>
{
    public async Task<List<ActivityParticipantDto>> Handle(
        ListActivityParticipantsQuery request, CancellationToken cancellationToken)
    {
        return await context.ActivityParticipants
            .Include(ap => ap.Person)
                .ThenInclude(p => p.VoiceGroup)
            .Where(ap => ap.ActivityId == request.ActivityId && ap.IsActive)
            .OrderBy(ap => ap.WaitlistPosition ?? int.MaxValue)
            .ThenBy(ap => ap.Person.LastName)
            .ThenBy(ap => ap.Person.FirstName)
            .Select(ap => new ActivityParticipantDto(
                ap.Id,
                ap.PersonId,
                ap.Person.FirstName + " " + ap.Person.LastName,
                ap.Person.VoiceGroup != null ? ap.Person.VoiceGroup.Name : null,
                ap.ExpectationStatus,
                ap.RsvpStatus,
                ap.SignupStatus,
                ap.WaitlistPosition,
                ap.Note))
            .ToListAsync(cancellationToken);
    }
}
