
namespace Arelia.Application.Finance.Queries;

public record GetCreditBalanceQuery(Guid PersonId) : IRequest<CreditBalanceDto?>;

public record CreditBalanceDto(
    decimal Balance,
    string CurrencyCode,
    List<CreditTransactionDto> RecentTransactions);

public record CreditTransactionDto(
    Guid Id,
    decimal Amount,
    string Reason,
    DateTime TransactionDate);

public class GetCreditBalanceHandler(IAreliaDbContext context)
    : IRequestHandler<GetCreditBalanceQuery, CreditBalanceDto?>
{
    public async Task<CreditBalanceDto?> Handle(
        GetCreditBalanceQuery request, CancellationToken cancellationToken)
    {
        var balance = await context.CreditBalances
            .FirstOrDefaultAsync(b => b.PersonId == request.PersonId, cancellationToken);

        if (balance is null)
            return null;

        var transactions = await context.CreditTransactions
            .Where(t => t.PersonId == request.PersonId && t.IsActive)
            .OrderByDescending(t => t.Timestamp)
            .Take(20)
            .Select(t => new CreditTransactionDto(t.Id, t.Amount, t.Reason, t.Timestamp))
            .ToListAsync(cancellationToken);

        return new CreditBalanceDto(balance.BalanceAmount, balance.CurrencyCode, transactions);
    }
}
