
namespace Arelia.Application.Activities.Queries;

public record GetActivityAttachmentsQuery(Guid ActivityId) : IRequest<List<ActivityAttachmentDto>>;

public record ActivityAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long? FileSizeBytes,
    DateTime UploadedAt);

public class GetActivityAttachmentsHandler(IAreliaDbContext context, IFileStorageService fileStorage)
    : IRequestHandler<GetActivityAttachmentsQuery, List<ActivityAttachmentDto>>
{
    public async Task<List<ActivityAttachmentDto>> Handle(
        GetActivityAttachmentsQuery request, CancellationToken cancellationToken)
    {
        var attachments = await context.ActivityAttachments
            .IgnoreQueryFilters()
            .Where(a => a.ActivityId == request.ActivityId && a.IsActive)
            .OrderBy(a => a.UploadedAt)
            .Select(a => new { a.Id, a.FileName, a.ContentType, a.FilePath, a.UploadedAt })
            .ToListAsync(cancellationToken);

        var result = new List<ActivityAttachmentDto>(attachments.Count);
        foreach (var a in attachments)
        {
            var size = await fileStorage.GetFileSizeAsync(a.FilePath, cancellationToken);
            result.Add(new ActivityAttachmentDto(a.Id, a.FileName, a.ContentType, size, a.UploadedAt));
        }

        return result;
    }
}
