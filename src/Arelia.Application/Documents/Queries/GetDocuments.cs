using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Documents.Queries;

public record GetDocumentsQuery(
    Guid OrganizationId,
    Guid? DocumentCategoryId = null,
    string? TitleSearch = null) : IRequest<List<DocumentSummaryDto>>;

public record DocumentSummaryDto(
    Guid Id,
    string Title,
    Guid? DocumentCategoryId,
    string? CategoryName,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt);

public class GetDocumentsHandler(IAreliaDbContext context)
    : IRequestHandler<GetDocumentsQuery, List<DocumentSummaryDto>>
{
    public async Task<List<DocumentSummaryDto>> Handle(
        GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Documents
            .IgnoreQueryFilters()
            .Where(d => d.OrganizationId == request.OrganizationId && d.IsActive);

        if (request.DocumentCategoryId.HasValue)
            query = query.Where(d => d.DocumentCategoryId == request.DocumentCategoryId.Value);

        if (!string.IsNullOrWhiteSpace(request.TitleSearch))
            query = query.Where(d => d.Title.Contains(request.TitleSearch));

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DocumentSummaryDto(
                d.Id,
                d.Title,
                d.DocumentCategoryId,
                d.Category != null ? d.Category.Name : null,
                d.CreatedAt,
                context.OrganizationUsers
                    .IgnoreQueryFilters()
                    .Where(ou => ou.UserId == d.CreatedBy && ou.OrganizationId == d.OrganizationId && ou.Person != null)
                    .Select(ou => ou.Person!.FirstName + " " + ou.Person!.LastName)
                    .FirstOrDefault(),
                d.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
