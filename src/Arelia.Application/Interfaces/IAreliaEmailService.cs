// Copyright (c) 2026 JBT Marel. All rights reserved.
namespace Arelia.Application.Interfaces;

/// <summary>
/// Arelia-specific transactional emails beyond the standard Identity email sender.
/// </summary>
public interface IAreliaEmailService
{
    /// <summary>
    /// Sends an invitation email to a new user with a "set your password" link.
    /// </summary>
    Task SendInvitationAsync(string toEmail, string orgName, string inviterName, string acceptLink);

    /// <summary>
    /// Sends a "you've been added to {orgName}" notification to an existing user.
    /// </summary>
    Task SendOrgAddedNotificationAsync(string toEmail, string orgName);
}
