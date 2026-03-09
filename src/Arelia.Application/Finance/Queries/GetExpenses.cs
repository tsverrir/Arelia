using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Finance.Queries;

public record GetExpensesQuery(Guid OrganizationId, int? Year = null, string? Category = null)
    : IRequest<List<ExpenseListDto>>;

public record ExpenseListDto(
    Guid Id,
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    string CategoryName,
    string? ReceivedBy,
    string CurrencyCode);

public class GetExpensesHandler(IAreliaDbContext context)
    : IRequestHandler<GetExpensesQuery, List<ExpenseListDto>>
{
    public async Task<List<ExpenseListDto>> Handle(
        GetExpensesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Expenses
            .Include(e => e.Category)
            .Where(e => e.OrganizationId == request.OrganizationId && e.IsActive);

        if (request.Year.HasValue)
            query = query.Where(e => e.ExpenseDate.Year == request.Year.Value);

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var normalized = request.Category.Trim().ToUpper();
            query = query.Where(e => e.Category.Name == normalized);
        }

        return await query
            .OrderByDescending(e => e.ExpenseDate)
            .Select(e => new ExpenseListDto(
                e.Id, e.Description, e.Amount,
                e.ExpenseDate, e.Category.Name,
                e.ReceivedBy, e.CurrencyCode))
            .ToListAsync(cancellationToken);
    }
}
