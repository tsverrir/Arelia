using Arelia.Domain.Enums;

namespace Arelia.Application.Finance.Queries;

public record GetChargesQuery(
    Guid OrganizationId,
    Guid? SemesterId = null,
    Guid? PersonId = null,
    bool IncludeHistory = false) : IRequest<List<ChargeListDto>>;

public record ChargeListDto(
    Guid Id,
    Guid PersonId,
    string PersonName,
    string Description,
    DateTime DueDate,
    ChargeStatus Status,
    decimal TotalDue,
    decimal TotalPaid,
    string CurrencyCode,
    bool IsActive,
    Guid? ReplacedById);

public class GetChargesHandler(IAreliaDbContext context)
    : IRequestHandler<GetChargesQuery, List<ChargeListDto>>
{
    public async Task<List<ChargeListDto>> Handle(
        GetChargesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Charges
            .IgnoreQueryFilters()
            .Include(c => c.ChargeLines)
            .Include(c => c.Payments)
            .Include(c => c.Person)
            .Where(c => c.OrganizationId == request.OrganizationId);

        if (!request.IncludeHistory)
            query = query.Where(c => c.IsActive);

        if (request.SemesterId.HasValue)
            query = query.Where(c => c.SemesterId == request.SemesterId);

        if (request.PersonId.HasValue)
            query = query.Where(c => c.PersonId == request.PersonId);

        var charges = await query
            .OrderBy(c => c.Person.LastName).ThenBy(c => c.DueDate)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return charges.Select(c => new ChargeListDto(
            c.Id,
            c.PersonId,
            c.Person.FullName,
            c.Description,
            c.DueDate,
            c.Status,
            c.TotalDue,
            c.TotalPaid,
            c.CurrencyCode,
            c.IsActive,
            c.ReplacedById)).ToList();
    }
}
