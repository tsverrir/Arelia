using Arelia.Domain.Common;
using Arelia.Domain.Entities;

namespace Arelia.Application.Activities.Commands;

public record AttachFileToActivityCommand(
    Guid ActivityId,
    string FileName,
    string ContentType,
    Stream FileContent,
    Guid OrganizationId) : IRequest<Result<Guid>>;

public class AttachFileToActivityHandler(
    IAreliaDbContext context,
    IFileStorageService fileStorage)
    : IRequestHandler<AttachFileToActivityCommand, Result<Guid>>
{
    private static readonly HashSet<string> AllowedMimeTypes =
    [
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "image/jpeg",
        "image/png",
        "image/gif",
    ];

    public async Task<Result<Guid>> Handle(AttachFileToActivityCommand request, CancellationToken cancellationToken)
    {
        if (!AllowedMimeTypes.Contains(request.ContentType.ToLowerInvariant()))
            return Result.Failure<Guid>($"File type '{request.ContentType}' is not allowed.");

        var activityExists = await context.Activities
            .AnyAsync(a => a.Id == request.ActivityId, cancellationToken);

        if (!activityExists)
            return Result.Failure<Guid>("Activity not found.");

        var relativePath = await fileStorage.SaveAsync(
            request.FileContent, request.FileName, request.ContentType,
            "activity-attachments", cancellationToken);

        var attachment = new ActivityAttachment
        {
            ActivityId = request.ActivityId,
            FileName = request.FileName,
            ContentType = request.ContentType,
            FilePath = relativePath,
            UploadedAt = DateTime.UtcNow,
            OrganizationId = request.OrganizationId,
        };

        context.ActivityAttachments.Add(attachment);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(attachment.Id);
    }
}
