using Arelia.Application.Interfaces;
using Arelia.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Documents.Commands;

public record UpdateDocumentCommand(
    Guid DocumentId,
    string Title,
    string ContentHtml,
    Guid? DocumentCategoryId) : IRequest<Result>;

public class UpdateDocumentHandler(IAreliaDbContext context, IHtmlSanitizerService sanitizer)
    : IRequestHandler<UpdateDocumentCommand, Result>
{
    public async Task<Result> Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure("Title is required.");

        var sanitized = sanitizer.Sanitize(request.ContentHtml ?? string.Empty);
        if (string.IsNullOrWhiteSpace(sanitized))
            return Result.Failure("Content is required.");

        var document = await context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);

        if (document is null)
            return Result.Failure("Document not found.");

        if (request.DocumentCategoryId.HasValue)
        {
            var categoryExists = await context.DocumentCategories
                .IgnoreQueryFilters()
                .AnyAsync(dc => dc.Id == request.DocumentCategoryId.Value &&
                                dc.OrganizationId == document.OrganizationId && dc.IsActive, cancellationToken);

            if (!categoryExists)
                return Result.Failure("Document category not found.");
        }

        document.Title = request.Title.Trim();
        document.ContentHtml = sanitized;
        document.DocumentCategoryId = request.DocumentCategoryId;

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
