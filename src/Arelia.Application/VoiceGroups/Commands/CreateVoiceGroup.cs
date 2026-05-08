using Arelia.Domain.Common;
using Arelia.Domain.Entities;

namespace Arelia.Application.VoiceGroups.Commands;

public record CreateVoiceGroupCommand(string Name, int SortOrder, Guid OrganizationId) : IRequest<Result<Guid>>;

public class CreateVoiceGroupHandler(IAreliaDbContext context)
    : IRequestHandler<CreateVoiceGroupCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVoiceGroupCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<Guid>("Name is required.");

        var exists = await context.VoiceGroups
            .IgnoreQueryFilters()
            .AnyAsync(v => v.OrganizationId == request.OrganizationId &&
                           v.Name == request.Name && v.IsActive, cancellationToken);

        if (exists)
            return Result.Failure<Guid>($"A voice group named '{request.Name}' already exists.");

        var voiceGroup = new VoiceGroup
        {
            Name = request.Name,
            SortOrder = request.SortOrder,
            OrganizationId = request.OrganizationId,
        };

        context.VoiceGroups.Add(voiceGroup);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(voiceGroup.Id);
    }
}
