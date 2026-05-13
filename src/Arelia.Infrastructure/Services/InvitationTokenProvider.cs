// Copyright (c) 2026 JBT Marel. All rights reserved.

using Arelia.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arelia.Infrastructure.Services;

/// <summary>
/// Token provider for invitation links that generates URL-safe Base64url tokens.
/// Wraps <see cref="DataProtectorTokenProvider{TUser}"/> and converts the output
/// to Base64url encoding (replacing + with -, / with _, stripping = padding)
/// so that tokens embed cleanly in URLs without double-encoding or client corruption.
/// </summary>
public class InvitationTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<DataProtectionTokenProviderOptions> options,
    ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
    : DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider, options, logger)
{
    /// <inheritdoc />
    public override async Task<string> GenerateAsync(
        string purpose, UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        var token = await base.GenerateAsync(purpose, manager, user);
        return ToBase64Url(token);
    }

    /// <inheritdoc />
    public override Task<bool> ValidateAsync(
        string purpose, string token,
        UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        return base.ValidateAsync(purpose, FromBase64Url(token), manager, user);
    }

    private static string ToBase64Url(string base64) =>
        base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string FromBase64Url(string base64Url)
    {
        var s = base64Url.Replace('-', '+').Replace('_', '/');
        var padding = (4 - s.Length % 4) % 4;
        return padding > 0 ? s + new string('=', padding) : s;
    }
}
