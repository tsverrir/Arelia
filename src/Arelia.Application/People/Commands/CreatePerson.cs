using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.People.Commands;

public record CreatePersonCommand(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    Guid? VoiceGroupId,
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
            VoiceGroupId = request.VoiceGroupId,
            Notes = request.Notes,
            OrganizationId = request.OrganizationId,
        };

        context.Persons.Add(person);

        var memberRole = await context.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.OrganizationId == request.OrganizationId &&
                                      r.Name == "Member" && r.IsActive, cancellationToken);

        if (memberRole is not null)
        {
            context.RoleAssignments.Add(new RoleAssignment
            {
                PersonId = person.Id,
                RoleId = memberRole.Id,
                FromDate = DateTime.UtcNow,
                OrganizationId = request.OrganizationId,
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return person.Id;
    }
}

