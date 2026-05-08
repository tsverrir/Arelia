using Arelia.Application.Common.Validation;
using Arelia.Domain.Entities;

namespace Arelia.Application.Organizations.Commands;

public record InviteUserCommand(
    string Email,
    Guid OrganizationId,
    string? FirstName,
    string? LastName) : IRequest<Domain.Common.Result>;

public class InviteUserHandler(
    IAreliaDbContext context,
    IUserService userService)
    : IRequestHandler<InviteUserCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Domain.Common.Result.Failure("Email address is required.");

        var normalizedEmail = request.Email.Trim();
        if (!InputValidation.IsValidEmail(normalizedEmail))
            return Domain.Common.Result.Failure("Email address is invalid.");

        var userId = await userService.FindUserIdByEmailAsync(normalizedEmail);

        if (userId is not null)
        {
            var alreadyLinked = await context.OrganizationUsers
                .IgnoreQueryFilters()
                .AnyAsync(ou => ou.UserId == userId && ou.OrganizationId == request.OrganizationId,
                    cancellationToken);

            if (alreadyLinked)
                return Domain.Common.Result.Failure("User is already a member of this organization.");
        }
        else
        {
            var createResult = await userService.CreateUserAsync(normalizedEmail);
            if (createResult.IsFailure)
                return Domain.Common.Result.Failure(createResult.Error!);
            userId = createResult.Value!;
        }

        var person = new Person
        {
            FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? "New" : request.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(request.LastName) ? "Member" : request.LastName.Trim(),
            Email = normalizedEmail,
            OrganizationId = request.OrganizationId,
        };
        context.Persons.Add(person);

        var orgUser = new OrganizationUser
        {
            UserId = userId,
            OrganizationId = request.OrganizationId,
            PersonId = person.Id,
            IsActive = true,
        };
        context.OrganizationUsers.Add(orgUser);

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

        return Domain.Common.Result.Success();
    }
}
