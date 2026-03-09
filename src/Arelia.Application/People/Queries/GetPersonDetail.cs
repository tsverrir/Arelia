using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.People.Queries;

public record GetPersonDetailQuery(Guid PersonId) : IRequest<PersonDetailDto?>;

public record PersonDetailDto(
	Guid Id,
	string FirstName,
	string LastName,
	string? Email,
	string? Phone,
	VoiceGroup? VoiceGroup,
	string? Notes,
	bool IsActive,
	List<PersonRoleAssignmentDto> RoleAssignments,
	bool HasLinkedUser);

public record PersonRoleAssignmentDto(
	Guid Id,
	string RoleName,
	RoleType RoleType,
	DateTime FromDate,
	DateTime? ToDate,
	bool IsCurrentlyActive);

public class GetPersonDetailHandler(IAreliaDbContext context)
	: IRequestHandler<GetPersonDetailQuery, PersonDetailDto?>
{
	public async Task<PersonDetailDto?> Handle(GetPersonDetailQuery request, CancellationToken cancellationToken)
	{
		var person = await context.Persons
			.IgnoreQueryFilters()
			.Where(p => p.Id == request.PersonId)
			.Select(p => new PersonDetailDto(
				p.Id,
				p.FirstName,
				p.LastName,
				p.Email,
				p.Phone,
				p.VoiceGroup,
				p.Notes,
				p.IsActive,
				p.RoleAssignments
					.Where(ra => ra.IsActive)
					.OrderByDescending(ra => ra.FromDate)   // ← moved before Select
					.Select(ra => new PersonRoleAssignmentDto(
						ra.Id,
						ra.Role.Name,
						ra.Role.RoleType,
						ra.FromDate,
						ra.ToDate,
						ra.FromDate <= DateTime.UtcNow && (ra.ToDate == null || ra.ToDate >= DateTime.UtcNow)))
					.ToList(),
				context.OrganizationUsers.IgnoreQueryFilters()
					.Any(ou => ou.PersonId == p.Id && ou.IsActive)))
			.FirstOrDefaultAsync(cancellationToken);

		return person;
	}
}
