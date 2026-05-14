using Arelia.Application.Common.Validation;
using Arelia.Domain.Common;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;

namespace Arelia.Application.Organizations.Commands;

/// <summary>
/// Invites a user to an organisation.
/// Path A (Person-first): <paramref name="PersonId"/> is provided — the person already exists and has no linked user.
/// Path B (Email-first): <paramref name="PersonId"/> is null — a new Person is created from the supplied email/name.
/// </summary>
public record InviteUserCommand(
    Guid OrganizationId,
    string? Email,
    string? FirstName,
    string? LastName,
    string? Phone,
    Guid? VoiceGroupId,
    string? Notes,
    Guid? PersonId,
    Guid? RoleId,
    string InviterName,
    string BaseUrl) : IRequest<Result>;

public class InviteUserHandler(
    IAreliaDbContext context,
    IUserService userService)
    : IRequestHandler<InviteUserCommand, Result>
{
    public async Task<Result> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        Guid resolvedPersonId;

        if (request.PersonId.HasValue)
        {
            // Path A: Person-first — use an existing unlinked person
            var existingPerson = await context.Persons
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == request.PersonId.Value
                                          && p.OrganizationId == request.OrganizationId
                                          && !p.IsDeleted, cancellationToken);

            if (existingPerson is null)
                return Result.Failure("Person not found in this organisation.");

            var alreadyLinked = await context.OrganizationUsers
                .IgnoreQueryFilters()
                .AnyAsync(ou => ou.PersonId == existingPerson.Id
                                && ou.OrganizationId == request.OrganizationId, cancellationToken);

            if (alreadyLinked)
                return Result.Failure("This person is already linked to a user in this organisation.");

            resolvedPersonId = existingPerson.Id;
        }
        else
        {
            // Path B: Email-first — email is required
            if (string.IsNullOrWhiteSpace(request.Email))
                return Result.Failure("Email address is required.");

            var normalizedEmail = request.Email.Trim();
            if (!InputValidation.IsValidEmail(normalizedEmail))
                return Result.Failure("Email address is invalid.");

            var userId = await userService.FindUserIdByEmailAsync(normalizedEmail);

            if (userId is not null)
            {
                // User exists — just add to org (no invite email needed, send notification instead)
                var alreadyMember = await context.OrganizationUsers
                    .IgnoreQueryFilters()
                    .AnyAsync(ou => ou.UserId == userId && ou.OrganizationId == request.OrganizationId,
                        cancellationToken);

                if (alreadyMember)
                    return Result.Failure("User is already a member of this organisation.");

                // Reuse an existing Person record for this email to avoid duplicates on re-submission
                var personForOrg = await context.Persons
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Email == normalizedEmail
                                              && p.OrganizationId == request.OrganizationId
                                              && !p.IsDeleted, cancellationToken);

                if (personForOrg is null)
                {
                    personForOrg = new Person
                    {
                        FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? "New" : request.FirstName.Trim(),
                        LastName = string.IsNullOrWhiteSpace(request.LastName) ? "Member" : request.LastName.Trim(),
                        Email = normalizedEmail,
                        Phone = request.Phone?.Trim(),
                        VoiceGroupId = request.VoiceGroupId,
                        Notes = request.Notes?.Trim(),
                        OrganizationId = request.OrganizationId,
                    };
                    context.Persons.Add(personForOrg);
                }
                resolvedPersonId = personForOrg.Id;

                var orgUserExisting = new OrganizationUser
                {
                    UserId = userId,
                    OrganizationId = request.OrganizationId,
                    PersonId = personForOrg.Id,
                };
                context.OrganizationUsers.Add(orgUserExisting);

                await AssignRoleAsync(resolvedPersonId, request.OrganizationId, request.RoleId, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                var org = await context.Organizations
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

                await userService.SendOrgAddedNotificationAsync(userId, org?.Name ?? "the organisation");
                return Result.Success();
            }

            // New user — create account and send invitation
            var createResult = await userService.CreateUserAsync(normalizedEmail);
            if (createResult.IsFailure)
                return Result.Failure(createResult.Error!);

            userId = createResult.Value!;

            // Reuse an orphaned Person record from a previous invite attempt if one exists
            var newPerson = await context.Persons
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Email == normalizedEmail
                                          && p.OrganizationId == request.OrganizationId
                                          && !p.IsDeleted, cancellationToken);

            if (newPerson is null)
            {
                newPerson = new Person
                {
                    FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? "New" : request.FirstName.Trim(),
                    LastName = string.IsNullOrWhiteSpace(request.LastName) ? "Member" : request.LastName.Trim(),
                    Email = normalizedEmail,
                    Phone = request.Phone?.Trim(),
                    VoiceGroupId = request.VoiceGroupId,
                    Notes = request.Notes?.Trim(),
                    OrganizationId = request.OrganizationId,
                };
                context.Persons.Add(newPerson);
            }
            resolvedPersonId = newPerson.Id;

            var orgUserNew = new OrganizationUser
            {
                UserId = userId,
                OrganizationId = request.OrganizationId,
                PersonId = newPerson.Id,
            };
            context.OrganizationUsers.Add(orgUserNew);

            await AssignRoleAsync(resolvedPersonId, request.OrganizationId, request.RoleId, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var orgForEmail = await context.Organizations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

            await userService.SendInvitationEmailAsync(
                userId, orgForEmail?.Name ?? "the organisation", request.InviterName, request.BaseUrl);

            return Result.Success();
        }

        // Path A continued: create OrgUser link for existing person
        // Determine user from person email (if any)
        string? resolvedUserId = null;
        var personForLink = await context.Persons
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == resolvedPersonId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(personForLink?.Email))
        {
            resolvedUserId = await userService.FindUserIdByEmailAsync(personForLink.Email);

            if (resolvedUserId is null)
            {
                var createResult = await userService.CreateUserAsync(personForLink.Email);
                if (createResult.IsFailure)
                    return Result.Failure(createResult.Error!);
                resolvedUserId = createResult.Value!;
            }
        }

        if (resolvedUserId is null)
            return Result.Failure("No email address is associated with this person. Cannot send an invitation.");

        var orgUserPathA = new OrganizationUser
        {
            UserId = resolvedUserId,
            OrganizationId = request.OrganizationId,
            PersonId = resolvedPersonId,
        };
        context.OrganizationUsers.Add(orgUserPathA);

        await AssignRoleAsync(resolvedPersonId, request.OrganizationId, request.RoleId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var orgForPathA = await context.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        var isPending = await userService.IsAccountPendingAsync(resolvedUserId);
        if (isPending)
        {
            await userService.SendInvitationEmailAsync(
                resolvedUserId, orgForPathA?.Name ?? "the organisation", request.InviterName, request.BaseUrl);
        }
        else
        {
            await userService.SendOrgAddedNotificationAsync(resolvedUserId, orgForPathA?.Name ?? "the organisation");
        }

        return Result.Success();
    }

    private async Task AssignRoleAsync(Guid personId, Guid organizationId, Guid? roleId, CancellationToken cancellationToken)
    {
        Role? role = null;

        if (roleId.HasValue)
        {
            role = await context.Roles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == roleId.Value && r.OrganizationId == organizationId, cancellationToken);
        }

        // Fall back to Member role if none specified or not found
        role ??= await context.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId
                                      && r.RoleType == RoleType.Member
                                      && r.IsActive, cancellationToken);

        if (role is null)
            return;

        context.RoleAssignments.Add(new RoleAssignment
        {
            PersonId = personId,
            RoleId = role.Id,
            FromDate = DateTime.Today,
            OrganizationId = organizationId,
        });
    }
}
