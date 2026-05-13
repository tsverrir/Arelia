using Arelia.Application.Interfaces;
using Arelia.Domain.Common;
using Arelia.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Arelia.Infrastructure.Services;

public class UserService(
    UserManager<ApplicationUser> userManager,
    IAreliaEmailService emailService) : IUserService
{
    public async Task<string?> FindUserIdByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user?.Id;
    }

    public async Task<Result<string>> CreateUserAsync(string email)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure<string>(errors);
        }

        return Result.Success(user.Id);
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        return await userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<string> GetUserDisplayNameAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user?.Email ?? userId;
    }

    public async Task<bool> IsAccountPendingAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && user.PasswordHash is null;
    }

    public async Task SendInvitationEmailAsync(string userId, string orgName, string inviterName, string baseUrl)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user?.Email is null)
            return;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var acceptLink = $"{baseUrl}/Account/AcceptInvitation?userId={Uri.EscapeDataString(userId)}&token={encodedToken}";
        await emailService.SendInvitationAsync(user.Email, orgName, inviterName, acceptLink);
    }

    public async Task SendOrgAddedNotificationAsync(string userId, string orgName)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user?.Email is null)
            return;

        await emailService.SendOrgAddedNotificationAsync(user.Email, orgName);
    }

    public async Task<string?> GetPreferredLanguageAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user?.PreferredLanguage;
    }

    public async Task SetPreferredLanguageAsync(string userId, string? language)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        user.PreferredLanguage = language;
        await userManager.UpdateAsync(user);
    }
}
