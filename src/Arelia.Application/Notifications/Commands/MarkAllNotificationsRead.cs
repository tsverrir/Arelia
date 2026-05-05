using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Notifications.Commands;

public record MarkAllNotificationsReadCommand(string UserId) : IRequest;

public class MarkAllNotificationsReadHandler(IAreliaDbContext context)
    : IRequestHandler<MarkAllNotificationsReadCommand>
{
    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var unread = await context.Notifications
            .IgnoreQueryFilters()
            .Where(n => n.RecipientUserId == request.UserId && !n.IsRead && n.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }

        if (unread.Count > 0)
            await context.SaveChangesAsync(cancellationToken);
    }
}
