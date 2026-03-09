using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using MediatR;

namespace Arelia.Application.People.Commands;

public record CreatePersonCommand(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    VoiceGroup? VoiceGroup,
    string? Notes,
    Guid OrganizationId) : IRequest<Guid>;

public class CreatePersonHandler(IAreliaDbContext context) : IRequestHandler<CreatePersonCommand, Guid>
{
    public async Task<Guid> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = new Person
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            VoiceGroup = request.VoiceGroup,
            Notes = request.Notes,
            OrganizationId = request.OrganizationId,
        };

        context.Persons.Add(person);
        await context.SaveChangesAsync(cancellationToken);

        return person.Id;
    }
}
