using Arelia.Domain.Common;
using Arelia.Domain.Entities;

namespace Arelia.Application.Documents.Commands;

public record CreateDocumentCommand(
    string Title,
    string ContentHtml,
    Guid? DocumentCategoryId,
    Guid OrganizationId) : IRequest<Result<Guid>>;

public class CreateDocumentHandler(IAreliaDbContext context, IHtmlSanitizerService sanitizer)
    : IRequestHandler<CreateDocumentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<Guid>("Title is required.");

        var sanitized = sanitizer.Sanitize(request.ContentHtml ?? string.Empty);
        if (string.IsNullOrWhiteSpace(sanitized))
            return Result.Failure<Guid>("Content is required.");

        if (request.DocumentCategoryId.HasValue)
        {
            var categoryExists = await context.DocumentCategories
                .IgnoreQueryFilters()
                .AnyAsync(dc => dc.Id == request.DocumentCategoryId.Value &&
                                dc.OrganizationId == request.OrganizationId && dc.IsActive, cancellationToken);

            if (!categoryExists)
                return Result.Failure<Guid>("Document category not found.");
        }

        var document = new Document
        {
            Title = request.Title.Trim(),
            ContentHtml = sanitized,
            DocumentCategoryId = request.DocumentCategoryId,
            OrganizationId = request.OrganizationId,
        };

        context.Documents.Add(document);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(document.Id);
    }
}
