using Arelia.Application.Interfaces;
using Arelia.Application.Common.Validation;
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
    Guid OrganizationId) : IRequest<Domain.Common.Result<Guid>>;

public class CreatePersonHandler(IAreliaDbContext context) : IRequestHandler<CreatePersonCommand, Domain.Common.Result<Guid>>
{
    public async Task<Domain.Common.Result<Guid>> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return Domain.Common.Result.Failure<Guid>("First name and last name are required.");

        if (!InputValidation.IsValidEmail(request.Email))
            return Domain.Common.Result.Failure<Guid>("Email address is invalid.");

        var person = new Person
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = InputValidation.NormalizeOptional(request.Email),
            Phone = InputValidation.NormalizeOptional(request.Phone),
            VoiceGroupId = request.VoiceGroupId,
            Notes = InputValidation.NormalizeOptional(request.Notes),
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

        return Domain.Common.Result.Success(person.Id);
    }
}

