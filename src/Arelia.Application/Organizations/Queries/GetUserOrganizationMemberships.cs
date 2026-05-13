//-------------------------------------------------------------------------------------------------
//
// GetUserOrganizationMemberships.cs -- The GetUserOrganizationMembershipsQuery class.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------

namespace Arelia.Application.Organizations.Queries;

/// <summary>Returns all organization memberships for a user, including their active roles in each org.</summary>
public record GetUserOrganizationMembershipsQuery(string UserId) : IRequest<List<UserOrgMembershipDto>>;

/// <summary>Org membership summary for a user, as seen from the System Admin view.</summary>
public record UserOrgMembershipDto(Guid OrgId, string OrgName, List<string> ActiveRoles);

/// <summary>Handles <see cref="GetUserOrganizationMembershipsQuery"/>.</summary>
public sealed class GetUserOrganizationMembershipsHandler(IAreliaDbContext context)
	: IRequestHandler<GetUserOrganizationMembershipsQuery, List<UserOrgMembershipDto>>
{
	//-----------------------------------------------------------------------------------------
	/// <inheritdoc />
	public async Task<List<UserOrgMembershipDto>> Handle(
		GetUserOrganizationMembershipsQuery request, CancellationToken cancellationToken)
	{
		var now = DateTime.UtcNow;

		var memberships = await context.OrganizationUsers
			.IgnoreQueryFilters()
			.Where(ou => ou.UserId == request.UserId)
			.Select(ou => new
			{
				ou.OrganizationId,
				OrgName = ou.Organization.Name,
				ActiveRoles = context.RoleAssignments
					.IgnoreQueryFilters()
					.Where(ra => ra.PersonId == ou.PersonId
						&& ra.OrganizationId == ou.OrganizationId
						&& (ra.ToDate == null || ra.ToDate > now))
					.Join(
						context.Roles.IgnoreQueryFilters(),
						ra => ra.RoleId,
						r => r.Id,
						(ra, r) => r.Name)
					.ToList(),
			})
			.OrderBy(m => m.OrgName)
			.ToListAsync(cancellationToken);

		return memberships
			.Select(m => new UserOrgMembershipDto(m.OrganizationId, m.OrgName, m.ActiveRoles))
			.ToList();
	}
}
