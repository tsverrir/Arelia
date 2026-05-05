using Arelia.Application.Interfaces;
using Arelia.Application.Common.Validation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.People.Commands;

public record UpdatePersonCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    Guid? VoiceGroupId,
    string? Notes) : IRequest<Domain.Common.Result>;

public class UpdatePersonHandler(IAreliaDbContext context) : IRequestHandler<UpdatePersonCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return Domain.Common.Result.Failure("First name and last name are required.");

        if (!InputValidation.IsValidEmail(request.Email))
            return Domain.Common.Result.Failure("Email address is invalid.");

        var person = await context.Persons.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (person is null)
            return Domain.Common.Result.Failure("Person not found.");

        person.FirstName = request.FirstName.Trim();
        person.LastName = request.LastName.Trim();
        person.Email = InputValidation.NormalizeOptional(request.Email);
        person.Phone = InputValidation.NormalizeOptional(request.Phone);
        person.VoiceGroupId = request.VoiceGroupId;
        person.Notes = InputValidation.NormalizeOptional(request.Notes);

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
