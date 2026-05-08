using Arelia.Domain.Common;

namespace Arelia.Application.Documents.Commands;

public enum DocumentCategoryDeleteMode
{
    UnassignDocuments,
    MoveDocuments,
}

public record DeleteDocumentCategoryCommand(
    Guid CategoryId,
    DocumentCategoryDeleteMode Mode,
    Guid? MoveToCategoryId = null) : IRequest<Result>;

public class DeleteDocumentCategoryHandler(IAreliaDbContext context)
    : IRequestHandler<DeleteDocumentCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteDocumentCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.DocumentCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(dc => dc.Id == request.CategoryId, cancellationToken);

        if (category is null)
            return Result.Failure("Document category not found.");

        var assignedDocuments = await context.Documents
            .IgnoreQueryFilters()
            .Where(d => d.DocumentCategoryId == request.CategoryId && d.IsActive)
            .ToListAsync(cancellationToken);

        if (assignedDocuments.Count > 0)
        {
            if (request.Mode == DocumentCategoryDeleteMode.MoveDocuments)
            {
                if (request.MoveToCategoryId is not Guid targetId)
                    return Result.Failure("A target category must be specified for move.");

                var targetExists = await context.DocumentCategories
                    .IgnoreQueryFilters()
                    .AnyAsync(dc => dc.Id == targetId && dc.IsActive, cancellationToken);

                if (!targetExists)
                    return Result.Failure("Target document category not found or is inactive.");

                foreach (var doc in assignedDocuments)
                    doc.DocumentCategoryId = targetId;
            }
            else
            {
                foreach (var doc in assignedDocuments)
                    doc.DocumentCategoryId = null;
            }
        }

        category.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
