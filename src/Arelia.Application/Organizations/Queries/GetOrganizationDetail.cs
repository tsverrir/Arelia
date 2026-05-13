//-------------------------------------------------------------------------------------------------
//
// GetOrganizationDetail.cs -- The GetOrganizationDetailQuery and handler.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------

namespace Arelia.Application.Organizations.Queries;

/// <summary>
/// Returns the full details of a single organisation. System Admin use only — bypasses tenant filter.
/// </summary>
public record GetOrganizationDetailQuery(Guid OrganizationId) : IRequest<OrganizationDetailDto?>;

/// <summary>
/// Full detail DTO for a single organisation, used on the System Admin detail page.
/// </summary>
public record OrganizationDetailDto(
	Guid Id,
	string Name,
	bool IsActive,
	string? ContactEmail,
	string? ContactPhone,
	string? DefaultLanguage,
	int MemberCount,
	DateTime CreatedAt);

/// <summary>
/// Handles <see cref="GetOrganizationDetailQuery"/>.
/// </summary>
public sealed class GetOrganizationDetailHandler(IAreliaDbContext context)
	: IRequestHandler<GetOrganizationDetailQuery, OrganizationDetailDto?>
{
	//-----------------------------------------------------------------------------------------
	/// <summary>
	/// Returns the organisation detail, or <c>null</c> if not found.
	/// </summary>
	public async Task<OrganizationDetailDto?> Handle(
		GetOrganizationDetailQuery request,
		CancellationToken cancellationToken)
	{
		return await context.Organizations
			.IgnoreQueryFilters()
			.Where(o => o.Id == request.OrganizationId)
			.Select(o => new OrganizationDetailDto(
				o.Id,
				o.Name,
				o.IsActive,
				o.ContactEmail,
				o.ContactPhone,
				o.DefaultLanguage,
				o.OrganizationUsers.Count,
				o.CreatedAt))
			.FirstOrDefaultAsync(cancellationToken);
	}
}
