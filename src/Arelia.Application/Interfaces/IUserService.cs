using Arelia.Domain.Common;

namespace Arelia.Application.Interfaces;

public interface IUserService
{
    /// <summary>Finds a user by email. Returns the user ID if found, null otherwise.</summary>
    Task<string?> FindUserIdByEmailAsync(string email);

    /// <summary>Creates a new pending user account (null password hash). Returns the user ID on success.</summary>
    Task<Result<string>> CreateUserAsync(string email);

    /// <summary>Generates a password-reset token for the user (used as the invite token).</summary>
    Task<string?> GeneratePasswordResetTokenAsync(string userId);

    /// <summary>Returns the user's display name (first + last name if set, otherwise email).</summary>
    Task<string> GetUserDisplayNameAsync(string userId);

    /// <summary>Returns true if the user's password hash is null (account is Pending).</summary>
    Task<bool> IsAccountPendingAsync(string userId);

    /// <summary>Sends an invitation email. Generates the reset token internally and builds the accept link.</summary>
    Task SendInvitationEmailAsync(string userId, string orgName, string inviterName, string baseUrl);

    /// <summary>Sends a "you've been added to {orgName}" notification email to an existing user.</summary>
    Task SendOrgAddedNotificationAsync(string userId, string orgName);

    /// <summary>Returns the user's preferred language. Null means no personal preference set.</summary>
    Task<string?> GetPreferredLanguageAsync(string userId);

    /// <summary>Persists the user's preferred language (null clears the preference).</summary>
    Task SetPreferredLanguageAsync(string userId, string? language);
}
