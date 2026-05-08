
namespace Arelia.Application.Admin.Queries;

public record GetAuditLogQuery(
    Guid OrganizationId,
    string? EntityType = null,
    string? Action = null,
    int Take = 50) : IRequest<List<AuditLogEntryDto>>;

public record AuditLogEntryDto(
    Guid Id,
    string Action,
    string? EntityType,
    string? EntityId,
    string? UserId,
    DateTime Timestamp,
    string? Details);

public class GetAuditLogHandler(IAreliaDbContext context)
    : IRequestHandler<GetAuditLogQuery, List<AuditLogEntryDto>>
{
    public async Task<List<AuditLogEntryDto>> Handle(
        GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        var query = context.AuditLogEntries
            .IgnoreQueryFilters()
            .Where(a => a.OrganizationId == request.OrganizationId);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action == request.Action);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(request.Take)
            .Select(a => new AuditLogEntryDto(
                a.Id, a.Action, a.EntityType, a.EntityId,
                a.UserId, a.Timestamp, a.Details))
            .ToListAsync(cancellationToken);
    }
}
