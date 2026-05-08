
namespace Arelia.Application.VoiceGroups.Commands;

public record UpdateVoiceGroupCommand(Guid Id, string Name, int SortOrder) : IRequest<Domain.Common.Result>;

public class UpdateVoiceGroupHandler(IAreliaDbContext context)
    : IRequestHandler<UpdateVoiceGroupCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(UpdateVoiceGroupCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Domain.Common.Result.Failure("Name is required.");

        var voiceGroup = await context.VoiceGroups
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (voiceGroup is null)
            return Domain.Common.Result.Failure("Voice group not found.");

        var duplicate = await context.VoiceGroups
            .IgnoreQueryFilters()
            .AnyAsync(v => v.OrganizationId == voiceGroup.OrganizationId &&
                           v.Name == request.Name && v.IsActive && v.Id != request.Id, cancellationToken);

        if (duplicate)
            return Domain.Common.Result.Failure($"A voice group named '{request.Name}' already exists.");

        voiceGroup.Name = request.Name;
        voiceGroup.SortOrder = request.SortOrder;

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
