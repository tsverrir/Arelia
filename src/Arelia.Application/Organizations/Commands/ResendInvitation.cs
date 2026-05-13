using Arelia.Domain.Common;

namespace Arelia.Application.Organizations.Commands;

/// <summary>
/// Resends an invitation to a user with a Pending Account (password not yet set).
/// Can be triggered by an admin from the person profile, or self-service from the expired invitation page.
/// </summary>
public record ResendInvitationCommand(
    string UserId,
    Guid OrganizationId,
    string InviterName,
    string BaseUrl) : IRequest<Result>;

public class ResendInvitationHandler(IAreliaDbContext context, IUserService userService)
    : IRequestHandler<ResendInvitationCommand, Result>
{
    public async Task<Result> Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
    {
        var isMember = await context.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(ou => ou.UserId == request.UserId
                            && ou.OrganizationId == request.OrganizationId, cancellationToken);

        if (!isMember)
            return Result.Failure("User is not a member of this organisation.");

        var isPending = await userService.IsAccountPendingAsync(request.UserId);
        if (!isPending)
            return Result.Failure("Account is already active. Invitations can only be resent to pending accounts.");

        var org = await context.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        await userService.SendInvitationEmailAsync(
            request.UserId, org?.Name ?? "the organisation", request.InviterName, request.BaseUrl);

        return Result.Success();
    }
}
