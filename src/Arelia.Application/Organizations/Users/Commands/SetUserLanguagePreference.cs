
namespace Arelia.Application.Organizations.Users.Commands;

public record SetUserLanguagePreferenceCommand(string UserId, string? Language) : IRequest<Unit>;

public class SetUserLanguagePreferenceHandler(IUserService userService)
    : IRequestHandler<SetUserLanguagePreferenceCommand, Unit>
{
    public async Task<Unit> Handle(SetUserLanguagePreferenceCommand request, CancellationToken cancellationToken)
    {
        await userService.SetPreferredLanguageAsync(request.UserId, request.Language);
        return Unit.Value;
    }
}
