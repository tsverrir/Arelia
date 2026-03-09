using Microsoft.AspNetCore.Identity;

namespace Arelia.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    /// <summary>User's preferred UI language (e.g. "en", "da", "is"). Null means use the organisation default.</summary>
    public string? PreferredLanguage { get; set; }
}
