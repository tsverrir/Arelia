using Arelia.Domain.Common;

namespace Arelia.Application.VoiceGroups.Commands;

public enum VoiceGroupDeleteMode
{
    UnassignPeople,
    MovePeople,
}

public record DeleteVoiceGroupCommand(
    Guid VoiceGroupId,
    VoiceGroupDeleteMode Mode,
    Guid? MoveToVoiceGroupId = null) : IRequest<Result>;

public class DeleteVoiceGroupHandler(IAreliaDbContext context)
    : IRequestHandler<DeleteVoiceGroupCommand, Result>
{
    public async Task<Result> Handle(DeleteVoiceGroupCommand request, CancellationToken cancellationToken)
    {
        var voiceGroup = await context.VoiceGroups
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == request.VoiceGroupId, cancellationToken);

        if (voiceGroup is null)
            return Result.Failure("Voice group not found.");

        var assignedPeople = await context.Persons
            .IgnoreQueryFilters()
            .Where(p => p.VoiceGroupId == request.VoiceGroupId && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        if (assignedPeople.Count > 0)
        {
            if (request.Mode == VoiceGroupDeleteMode.MovePeople)
            {
                if (request.MoveToVoiceGroupId is not Guid targetId)
                    return Result.Failure("A target voice group must be specified for move.");

                var targetExists = await context.VoiceGroups
                    .IgnoreQueryFilters()
                    .AnyAsync(v => v.Id == targetId && v.IsActive, cancellationToken);

                if (!targetExists)
                    return Result.Failure("Target voice group not found or is inactive.");

                foreach (var person in assignedPeople)
                    person.VoiceGroupId = targetId;
            }
            else
            {
                foreach (var person in assignedPeople)
                    person.VoiceGroupId = null;
            }
        }

        voiceGroup.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
