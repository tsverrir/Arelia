using Arelia.Domain.Common;

namespace Arelia.Application.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Finds a user by email. Returns the user ID if found, null otherwise.
    /// </summary>
    Task<string?> FindUserIdByEmailAsync(string email);

    /// <summary>
    /// Creates a new user account with the given email. Returns the user ID on success.
    /// </summary>
    Task<Result<string>> CreateUserAsync(string email);

    /// <summary>
    /// Generates a password reset token for the user.
    /// </summary>
    Task<string?> GeneratePasswordResetTokenAsync(string userId);

    /// <summary>
    /// Returns the user's preferred language. Null means no personal preference set.
    /// </summary>
    Task<string?> GetPreferredLanguageAsync(string userId);

    /// <summary>
    /// Persists the user's preferred language (null clears the preference, falling back to org default).
    /// </summary>
    Task SetPreferredLanguageAsync(string userId, string? language);
}
