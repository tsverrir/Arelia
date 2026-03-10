using Arelia.Application.Interfaces;
using Arelia.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Documents.Commands;

public record DeleteDocumentCommand(Guid DocumentId) : IRequest<Result>;

public class DeleteDocumentHandler(IAreliaDbContext context)
    : IRequestHandler<DeleteDocumentCommand, Result>
{
    public async Task<Result> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);

        if (document is null)
            return Result.Failure("Document not found.");

        document.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
