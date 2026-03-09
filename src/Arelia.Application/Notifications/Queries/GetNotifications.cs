using Arelia.Application.Interfaces;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Notifications.Queries;

public record GetNotificationsQuery(string UserId, int Take = 20) : IRequest<List<NotificationDto>>;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    string? LinkUrl,
    bool IsRead,
    DateTime CreatedAt);

public class GetNotificationsHandler(IAreliaDbContext context)
    : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    public async Task<List<NotificationDto>> Handle(
        GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        return await context.Notifications
            .IgnoreQueryFilters()
            .Where(n => n.RecipientUserId == request.UserId && n.IsActive)
            .OrderByDescending(n => n.CreatedAt)
            .Take(request.Take)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.Message, n.LinkUrl, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
