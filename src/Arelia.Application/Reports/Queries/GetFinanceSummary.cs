
namespace Arelia.Application.Reports.Queries;

public record GetFinanceSummaryQuery(Guid OrganizationId, int Year)
    : IRequest<FinanceSummaryDto>;

public record FinanceSummaryDto(
    decimal TotalChargesDue,
    decimal TotalChargesPaid,
    decimal TotalExpenses,
    decimal NetBalance,
    List<ExpenseByCategoryRow> ExpensesByCategory,
    List<CreditBalanceRow> CreditBalances,
    int OpenChargeCount,
    int OverdueChargeCount);

public record ExpenseByCategoryRow(string Category, decimal Amount);
public record CreditBalanceRow(Guid PersonId, string PersonName, decimal BalanceAmount, string CurrencyCode);

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
        var openCount = charges.Count(c => c.Status is Domain.Enums.ChargeStatus.Open or Domain.Enums.ChargeStatus.PartiallyPaid);
        var overdueCount = charges.Count(c =>
            c.DueDate < DateTime.UtcNow &&
            c.Status is Domain.Enums.ChargeStatus.Open or Domain.Enums.ChargeStatus.PartiallyPaid);

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

        var creditBalances = await context.CreditBalances
            .Include(cb => cb.Person)
            .Where(cb => cb.OrganizationId == request.OrganizationId && cb.BalanceAmount != 0)
            .OrderBy(cb => cb.Person.LastName)
            .ThenBy(cb => cb.Person.FirstName)
            .Select(cb => new CreditBalanceRow(
                cb.PersonId,
                cb.Person.FirstName + " " + cb.Person.LastName,
                cb.BalanceAmount,
                cb.CurrencyCode))
            .ToListAsync(cancellationToken);

        return new FinanceSummaryDto(
            totalDue, totalPaid, totalExpenses,
            totalPaid - totalExpenses,
            byCategory, creditBalances, openCount, overdueCount);
    }
}
