using Arelia.Domain.Common;

namespace Arelia.Application.Activities.Commands;

public record DeleteActivityAttachmentCommand(Guid AttachmentId) : IRequest<Result>;

public class DeleteActivityAttachmentHandler(IAreliaDbContext context, IFileStorageService fileStorage)
    : IRequestHandler<DeleteActivityAttachmentCommand, Result>
{
    public async Task<Result> Handle(DeleteActivityAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = await context.ActivityAttachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId, cancellationToken);

        if (attachment is null)
            return Result.Failure("Attachment not found.");

        await fileStorage.DeleteAsync(attachment.FilePath, cancellationToken);

        attachment.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
