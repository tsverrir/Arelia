
namespace Arelia.Application.People.Commands;

public record DeletePersonCommand(Guid PersonId) : IRequest<Domain.Common.Result>;

public class DeletePersonHandler(IAreliaDbContext context) : IRequestHandler<DeletePersonCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await context.Persons
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

        if (person is null)
            return Domain.Common.Result.Failure("Person not found.");

        person.IsDeleted = true;
        person.IsActive = false;

        // Also deactivate linked OrganizationUser if any
        var orgUser = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ou => ou.PersonId == request.PersonId && ou.IsActive, cancellationToken);

        if (orgUser is not null)
            orgUser.IsActive = false;

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
