using Arelia.Application.Interfaces;
using Arelia.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Documents.Commands;

public record UpdateDocumentCategoryCommand(Guid Id, string Name, int SortOrder) : IRequest<Result>;

public class UpdateDocumentCategoryHandler(IAreliaDbContext context)
    : IRequestHandler<UpdateDocumentCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateDocumentCategoryCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure("Name is required.");

        var category = await context.DocumentCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(dc => dc.Id == request.Id, cancellationToken);

        if (category is null)
            return Result.Failure("Document category not found.");

        var duplicate = await context.DocumentCategories
            .IgnoreQueryFilters()
            .AnyAsync(dc => dc.OrganizationId == category.OrganizationId &&
                            dc.Name == request.Name.Trim() && dc.IsActive && dc.Id != request.Id, cancellationToken);

        if (duplicate)
            return Result.Failure($"A document category named '{request.Name}' already exists.");

        category.Name = request.Name.Trim();
        category.SortOrder = request.SortOrder;

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
