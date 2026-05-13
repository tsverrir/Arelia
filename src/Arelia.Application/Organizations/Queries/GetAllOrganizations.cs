// Copyright (c) 2026 JBT Marel. All rights reserved.
namespace Arelia.Application.Organizations.Queries;

/// <summary>
/// Returns all organisations in the system. System Admin use only — bypasses tenant filter.
/// </summary>
public record GetAllOrganizationsQuery : IRequest<List<OrganizationSummaryDto>>;

public record OrganizationSummaryDto(
    Guid Id,
    string Name,
    bool IsActive,
    int MemberCount,
    DateTime CreatedAt);

public class GetAllOrganizationsHandler(IAreliaDbContext context)
    : IRequestHandler<GetAllOrganizationsQuery, List<OrganizationSummaryDto>>
{
    public async Task<List<OrganizationSummaryDto>> Handle(
        GetAllOrganizationsQuery request, CancellationToken cancellationToken)
    {
        return await context.Organizations
            .IgnoreQueryFilters()
            .OrderBy(o => o.Name)
            .Select(o => new OrganizationSummaryDto(
                o.Id,
                o.Name,
                o.IsActive,
                o.OrganizationUsers.Count,
                o.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
