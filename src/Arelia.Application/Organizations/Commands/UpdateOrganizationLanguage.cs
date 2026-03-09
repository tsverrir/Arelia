using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Organizations.Commands;

public record UpdateOrganizationLanguageCommand(Guid OrganizationId, string? Language) : IRequest;

public class UpdateOrganizationLanguageHandler(IAreliaDbContext context)
    : IRequestHandler<UpdateOrganizationLanguageCommand>
{
    public async Task Handle(UpdateOrganizationLanguageCommand request, CancellationToken cancellationToken)
    {
        var org = await context.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken)
            ?? throw new InvalidOperationException($"Organization {request.OrganizationId} not found.");

        org.DefaultLanguage = request.Language;
        await context.SaveChangesAsync(cancellationToken);
    }
}
