using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Documents.Queries;

public record GetDocumentDetailQuery(Guid DocumentId) : IRequest<DocumentDetailDto?>;

public record DocumentDetailDto(
    Guid Id,
    Guid OrganizationId,
    string Title,
    string ContentHtml,
    Guid? DocumentCategoryId,
    string? CategoryName,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy);

public class GetDocumentDetailHandler(IAreliaDbContext context)
    : IRequestHandler<GetDocumentDetailQuery, DocumentDetailDto?>
{
    public async Task<DocumentDetailDto?> Handle(
        GetDocumentDetailQuery request, CancellationToken cancellationToken)
    {
        return await context.Documents
            .IgnoreQueryFilters()
            .Where(d => d.Id == request.DocumentId && d.IsActive)
            .Select(d => new DocumentDetailDto(
                d.Id,
                d.OrganizationId,
                d.Title,
                d.ContentHtml,
                d.DocumentCategoryId,
                d.Category != null ? d.Category.Name : null,
                d.CreatedAt,
                context.OrganizationUsers
                    .IgnoreQueryFilters()
                    .Where(ou => ou.UserId == d.CreatedBy && ou.OrganizationId == d.OrganizationId && ou.Person != null)
                    .Select(ou => ou.Person!.FirstName + " " + ou.Person!.LastName)
                    .FirstOrDefault(),
                d.UpdatedAt,
                context.OrganizationUsers
                    .IgnoreQueryFilters()
                    .Where(ou => ou.UserId == d.UpdatedBy && ou.OrganizationId == d.OrganizationId && ou.Person != null)
                    .Select(ou => ou.Person!.FirstName + " " + ou.Person!.LastName)
                    .FirstOrDefault()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
