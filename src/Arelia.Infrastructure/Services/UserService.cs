using Arelia.Application.Interfaces;
using Arelia.Domain.Common;
using Arelia.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Arelia.Infrastructure.Services;

public class UserService(UserManager<ApplicationUser> userManager) : IUserService
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
}
