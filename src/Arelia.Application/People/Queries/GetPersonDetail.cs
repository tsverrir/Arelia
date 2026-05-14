


namespace Arelia.Application.People.Queries;

public record GetPersonDetailQuery(Guid PersonId) : IRequest<PersonDetailDto?>;

public record PersonDetailDto(
	Guid Id,
	string FirstName,
	string LastName,
	string? Email,
	string? Phone,
	Guid? VoiceGroupId,
	string? VoiceGroupName,
	string? Notes,
	bool IsActive,
	List<PersonRoleAssignmentDto> RoleAssignments,
	bool HasLinkedUser,
	string? LinkedUserId,
	Guid? OrganizationUserId);

public record PersonRoleAssignmentDto(
	Guid Id,
	string RoleName,
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
			.Where(p => p.Id == request.PersonId && !p.IsDeleted)
			.Select(p => new PersonDetailDto(
				p.Id,
				p.FirstName,
				p.LastName,
				p.Email,
				p.Phone,
				p.VoiceGroupId,
				p.VoiceGroup != null ? p.VoiceGroup.Name : null,
				p.Notes,
				p.IsActive,
				p.RoleAssignments
					.Where(ra => ra.IsActive)
					.OrderByDescending(ra => ra.FromDate)   // ← moved before Select
					.Select(ra => new PersonRoleAssignmentDto(
						ra.Id,
						ra.Role.Name,
						ra.FromDate,
						ra.ToDate,
						ra.FromDate <= DateTime.Today && (ra.ToDate == null || ra.ToDate > DateTime.Today)))
					.ToList(),
				context.OrganizationUsers.IgnoreQueryFilters()
					.Any(ou => ou.PersonId == p.Id),
				context.OrganizationUsers.IgnoreQueryFilters()
					.Where(ou => ou.PersonId == p.Id)
					.Select(ou => (string?)ou.UserId)
					.FirstOrDefault(),
				context.OrganizationUsers.IgnoreQueryFilters()
					.Where(ou => ou.PersonId == p.Id)
					.Select(ou => (Guid?)ou.Id)
					.FirstOrDefault()))
			.FirstOrDefaultAsync(cancellationToken);

		return person;
	}
}
