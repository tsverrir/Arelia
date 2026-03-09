using Arelia.Domain.Enums;

namespace Arelia.Application.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Creates an in-app notification and optionally sends email.
    /// </summary>
    Task SendAsync(
        string recipientUserId,
        NotificationType type,
        string title,
        string message,
        string? linkUrl = null,
        bool sendEmail = false,
        CancellationToken cancellationToken = default);
}
