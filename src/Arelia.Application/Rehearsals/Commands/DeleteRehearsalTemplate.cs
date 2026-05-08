
namespace Arelia.Application.Rehearsals.Commands;

public record DeleteRehearsalTemplateCommand(Guid TemplateId) : IRequest<Domain.Common.Result>;

public class DeleteRehearsalTemplateHandler(IAreliaDbContext context)
    : IRequestHandler<DeleteRehearsalTemplateCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        DeleteRehearsalTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await context.RehearsalRecurrenceTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (template is null)
            return Domain.Common.Result.Failure("Rehearsal template not found.");

        template.IsActive = false;

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
