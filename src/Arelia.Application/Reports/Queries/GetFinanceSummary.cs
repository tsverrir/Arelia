using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Reports.Queries;

public record GetFinanceSummaryQuery(Guid OrganizationId, int Year)
    : IRequest<FinanceSummaryDto>;

public record FinanceSummaryDto(
    decimal TotalChargesDue,
    decimal TotalChargesPaid,
    decimal TotalExpenses,
    decimal NetBalance,
    List<ExpenseByCategoryRow> ExpensesByCategory,
    int OpenChargeCount,
    int OverdueChargeCount);

public record ExpenseByCategoryRow(string Category, decimal Amount);

public class GetFinanceSummaryHandler(IAreliaDbContext context)
    : IRequestHandler<GetFinanceSummaryQuery, FinanceSummaryDto>
{
    public async Task<FinanceSummaryDto> Handle(
        GetFinanceSummaryQuery request, CancellationToken cancellationToken)
    {
        var charges = await context.Charges
            .Include(c => c.ChargeLines)
            .Include(c => c.Payments)
            .Where(c => c.OrganizationId == request.OrganizationId && c.DueDate.Year == request.Year)
            .ToListAsync(cancellationToken);

        var totalDue = charges.Sum(c => c.TotalDue);
        var totalPaid = charges.Sum(c => c.TotalPaid);
        var openCount = charges.Count(c => c.Status != Domain.Enums.ChargeStatus.Paid);
        var overdueCount = charges.Count(c =>
            c.DueDate < DateTime.UtcNow && c.Status != Domain.Enums.ChargeStatus.Paid);

        var expenses = await context.Expenses
            .Include(e => e.Category)
            .Where(e => e.OrganizationId == request.OrganizationId && e.ExpenseDate.Year == request.Year && e.IsActive)
            .ToListAsync(cancellationToken);

        var totalExpenses = expenses.Sum(e => e.Amount);
        var byCategory = expenses
            .GroupBy(e => e.Category.Name)
            .Select(g => new ExpenseByCategoryRow(g.Key, g.Sum(e => e.Amount)))
            .OrderByDescending(r => r.Amount)
            .ToList();

        return new FinanceSummaryDto(
            totalDue, totalPaid, totalExpenses,
            totalPaid - totalExpenses,
            byCategory, openCount, overdueCount);
    }
}
