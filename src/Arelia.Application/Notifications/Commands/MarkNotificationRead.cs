
namespace Arelia.Application.Notifications.Commands;

public record MarkNotificationReadCommand(Guid NotificationId, string UserId) : IRequest<Unit>;

public class MarkNotificationReadHandler(IAreliaDbContext context)
    : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    public async Task<Unit> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await context.Notifications
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.RecipientUserId == request.UserId,
                cancellationToken);

        if (notification is not null)
        {
            notification.IsRead = true;
            await context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
