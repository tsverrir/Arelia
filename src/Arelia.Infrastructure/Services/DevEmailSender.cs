using Arelia.Application.Interfaces;
using Arelia.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Arelia.Infrastructure.Services;

public class DevEmailSender(ILogger<DevEmailSender> logger) : IEmailSender<ApplicationUser>, IAreliaEmailService
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        logger.LogInformation("[DEV EMAIL] Confirmation link for {Email}: {Link}", email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        logger.LogInformation("[DEV EMAIL] Password reset link for {Email}: {Link}", email, resetLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        logger.LogInformation("[DEV EMAIL] Password reset code for {Email}: {Code}", email, resetCode);
        return Task.CompletedTask;
    }

    public Task SendInvitationAsync(string toEmail, string orgName, string inviterName, string acceptLink)
    {
        logger.LogInformation("[DEV EMAIL] Invitation to {OrgName} for {Email} from {Inviter}: {Link}", orgName, toEmail, inviterName, acceptLink);
        return Task.CompletedTask;
    }

    public Task SendOrgAddedNotificationAsync(string toEmail, string orgName)
    {
        logger.LogInformation("[DEV EMAIL] Added to {OrgName} notification for {Email}", orgName, toEmail);
        return Task.CompletedTask;
    }
}
