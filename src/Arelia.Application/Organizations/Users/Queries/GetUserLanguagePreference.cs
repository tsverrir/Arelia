
namespace Arelia.Application.Organizations.Users.Queries;

/// <summary>Returns the effective UI language: user pref → org default → "en".</summary>
public record GetUserLanguagePreferenceQuery(string UserId, Guid? OrganizationId) : IRequest<string>;

public class GetUserLanguagePreferenceHandler(IUserService userService, IAreliaDbContext context)
    : IRequestHandler<GetUserLanguagePreferenceQuery, string>
{
    public async Task<string> Handle(
        GetUserLanguagePreferenceQuery request,
        CancellationToken cancellationToken)
    {
        var userPref = await userService.GetPreferredLanguageAsync(request.UserId);
        if (!string.IsNullOrWhiteSpace(userPref))
            return userPref;

        if (request.OrganizationId is Guid orgId)
        {
            var orgLanguage = await context.Organizations
                .IgnoreQueryFilters()
                .Where(o => o.Id == orgId)
                .Select(o => o.DefaultLanguage)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(orgLanguage))
                return orgLanguage;
        }

        return "en";
    }
}
