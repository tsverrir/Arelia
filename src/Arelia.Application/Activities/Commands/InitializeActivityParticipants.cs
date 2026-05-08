using Arelia.Domain.Entities;
using Arelia.Domain.Enums;

namespace Arelia.Application.Activities.Commands;

public record InitializeActivityParticipantsCommand(
    Guid ActivityId,
    Guid OrganizationId,
    ParticipationExpectationStatus DefaultExpectation = ParticipationExpectationStatus.Expected)
    : IRequest<Domain.Common.Result<int>>;

public class InitializeActivityParticipantsHandler(IAreliaDbContext context)
    : IRequestHandler<InitializeActivityParticipantsCommand, Domain.Common.Result<int>>
{
    public async Task<Domain.Common.Result<int>> Handle(
        InitializeActivityParticipantsCommand request, CancellationToken cancellationToken)
    {
        var activity = await context.Activities
            .FirstOrDefaultAsync(a =>
                a.Id == request.ActivityId &&
                a.OrganizationId == request.OrganizationId,
                cancellationToken);

        if (activity is null)
            return Domain.Common.Result.Failure<int>("Activity not found.");

        var existingPersonIds = await context.ActivityParticipants
            .IgnoreQueryFilters()
            .Where(ap =>
                ap.ActivityId == activity.Id &&
                ap.OrganizationId == request.OrganizationId &&
                ap.IsActive)
            .Select(ap => ap.PersonId)
            .ToListAsync(cancellationToken);

        var memberIds = await context.Persons
            .Where(p =>
                p.OrganizationId == request.OrganizationId &&
                p.IsActive &&
                context.RoleAssignments.Any(ra =>
                    ra.PersonId == p.Id &&
                    ra.IsActive &&
                    ra.Role.Name == "Member" &&
                    ra.FromDate <= activity.StartDateTime &&
                    (ra.ToDate == null || ra.ToDate >= activity.StartDateTime)))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var existing = existingPersonIds.ToHashSet();
        var created = 0;

        foreach (var personId in memberIds.Where(id => !existing.Contains(id)))
        {
            context.ActivityParticipants.Add(new ActivityParticipant
            {
                ActivityId = activity.Id,
                PersonId = personId,
                ExpectationStatus = request.DefaultExpectation,
                SignupStatus = SignupStatus.None,
                RsvpStatus = RsvpStatus.Unanswered,
                OrganizationId = request.OrganizationId,
            });
            created++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success(created);
    }
}
