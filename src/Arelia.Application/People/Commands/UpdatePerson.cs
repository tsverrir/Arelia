using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.People.Commands;

public record UpdatePersonCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    VoiceGroup? VoiceGroup,
    string? Notes) : IRequest<Domain.Common.Result>;

public class UpdatePersonHandler(IAreliaDbContext context) : IRequestHandler<UpdatePersonCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await context.Persons.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (person is null)
            return Domain.Common.Result.Failure("Person not found.");

        person.FirstName = request.FirstName;
        person.LastName = request.LastName;
        person.Email = request.Email;
        person.Phone = request.Phone;
        person.VoiceGroup = request.VoiceGroup;
        person.Notes = request.Notes;

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
