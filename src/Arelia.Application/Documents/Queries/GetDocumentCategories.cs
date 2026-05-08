
namespace Arelia.Application.Documents.Queries;

public record GetDocumentCategoriesQuery(Guid OrganizationId) : IRequest<List<DocumentCategoryDto>>;

public record DocumentCategoryDto(Guid Id, string Name, int SortOrder, int DocumentCount);

public class GetDocumentCategoriesHandler(IAreliaDbContext context)
    : IRequestHandler<GetDocumentCategoriesQuery, List<DocumentCategoryDto>>
{
    public async Task<List<DocumentCategoryDto>> Handle(
        GetDocumentCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await context.DocumentCategories
            .IgnoreQueryFilters()
            .Where(dc => dc.OrganizationId == request.OrganizationId && dc.IsActive)
            .OrderBy(dc => dc.SortOrder)
            .ThenBy(dc => dc.Name)
            .Select(dc => new DocumentCategoryDto(
                dc.Id,
                dc.Name,
                dc.SortOrder,
                dc.Documents.Count(d => d.IsActive)))
            .ToListAsync(cancellationToken);
    }
}
