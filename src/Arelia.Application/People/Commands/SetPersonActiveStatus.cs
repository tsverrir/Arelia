using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.People.Commands;

public record SetPersonActiveStatusCommand(Guid PersonId, bool IsActive) : IRequest<Domain.Common.Result>;

public class SetPersonActiveStatusHandler(IAreliaDbContext context)
    : IRequestHandler<SetPersonActiveStatusCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(SetPersonActiveStatusCommand request, CancellationToken cancellationToken)
    {
        var person = await context.Persons
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == request.PersonId && !p.IsDeleted, cancellationToken);

        if (person is null)
            return Domain.Common.Result.Failure("Person not found.");

        person.IsActive = request.IsActive;

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
