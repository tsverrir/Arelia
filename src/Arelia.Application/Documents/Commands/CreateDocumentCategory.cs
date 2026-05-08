using Arelia.Domain.Common;
using Arelia.Domain.Entities;

namespace Arelia.Application.Documents.Commands;

public record CreateDocumentCategoryCommand(string Name, int SortOrder, Guid OrganizationId) : IRequest<Result<Guid>>;

public class CreateDocumentCategoryHandler(IAreliaDbContext context)
    : IRequestHandler<CreateDocumentCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateDocumentCategoryCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<Guid>("Name is required.");

        var exists = await context.DocumentCategories
            .IgnoreQueryFilters()
            .AnyAsync(dc => dc.OrganizationId == request.OrganizationId &&
                            dc.Name == request.Name.Trim() && dc.IsActive, cancellationToken);

        if (exists)
            return Result.Failure<Guid>($"A document category named '{request.Name}' already exists.");

        var category = new DocumentCategory
        {
            Name = request.Name.Trim(),
            SortOrder = request.SortOrder,
            OrganizationId = request.OrganizationId,
        };

        context.DocumentCategories.Add(category);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(category.Id);
    }
}
