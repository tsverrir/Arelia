
namespace Arelia.Application.Organizations.Commands;

public record UpdateOrganizationLanguageCommand(Guid OrganizationId, string? Language) : IRequest<Unit>;

public class UpdateOrganizationLanguageHandler(IAreliaDbContext context)
    : IRequestHandler<UpdateOrganizationLanguageCommand, Unit>
{
    public async Task<Unit> Handle(UpdateOrganizationLanguageCommand request, CancellationToken cancellationToken)
    {
        var org = await context.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken)
            ?? throw new InvalidOperationException($"Organization {request.OrganizationId} not found.");

        org.DefaultLanguage = request.Language;
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
