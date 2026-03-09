using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using Arelia.Infrastructure.Identity;
using Arelia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace Arelia.Infrastructure.Services;

public class NotificationService(
    AreliaDbContext context,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task SendAsync(
        string recipientUserId,
        NotificationType type,
        string title,
        string message,
        string? linkUrl = null,
        bool sendEmail = false,
        CancellationToken cancellationToken = default)
    {
        // In-app notification
        context.Notifications.Add(new Notification
        {
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
        });

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Notification sent to {UserId}: [{Type}] {Title}", recipientUserId, type, title);
    }
}
