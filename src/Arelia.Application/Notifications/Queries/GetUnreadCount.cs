using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Notifications.Queries;

public record GetUnreadCountQuery(string UserId) : IRequest<int>;

public class GetUnreadCountHandler(IAreliaDbContext context)
    : IRequestHandler<GetUnreadCountQuery, int>
{
    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        return await context.Notifications
            .IgnoreQueryFilters()
            .CountAsync(n => n.RecipientUserId == request.UserId && !n.IsRead && n.IsActive,
                cancellationToken);
    }
}
