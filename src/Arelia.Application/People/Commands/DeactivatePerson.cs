
namespace Arelia.Application.People.Commands;

public record DeactivatePersonCommand(Guid PersonId) : IRequest<Domain.Common.Result>;

public class DeactivatePersonHandler(IAreliaDbContext context) : IRequestHandler<DeactivatePersonCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(DeactivatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await context.Persons.FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);
        if (person is null)
            return Domain.Common.Result.Failure("Person not found.");

        person.IsActive = false;
        var today = DateTime.UtcNow;

        var activeRoleAssignments = await context.RoleAssignments
            .IgnoreQueryFilters()
            .Where(ra =>
                ra.PersonId == request.PersonId &&
                ra.IsActive &&
                ra.ToDate == null)
            .ToListAsync(cancellationToken);

        foreach (var assignment in activeRoleAssignments)
            assignment.ToDate = today;

        // Also deactivate linked OrganizationUser if any
        var orgUser = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ou => ou.PersonId == request.PersonId && ou.IsActive, cancellationToken);

        if (orgUser is not null)
        {
            orgUser.IsActive = false;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
