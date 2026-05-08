
namespace Arelia.Application.Organizations.Users.Commands;

public record SetUserLanguagePreferenceCommand(string UserId, string? Language) : IRequest;

public class SetUserLanguagePreferenceHandler(IUserService userService)
    : IRequestHandler<SetUserLanguagePreferenceCommand>
{
    public Task Handle(SetUserLanguagePreferenceCommand request, CancellationToken cancellationToken) =>
        userService.SetPreferredLanguageAsync(request.UserId, request.Language);
}
