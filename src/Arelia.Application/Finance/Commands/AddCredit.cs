using Arelia.Domain.Entities;

namespace Arelia.Application.Finance.Commands;

public record AddCreditCommand(
    Guid PersonId,
    decimal Amount,
    string Reason,
    Guid? PaymentId,
    Guid? ChargeId,
    Guid OrganizationId) : IRequest<Domain.Common.Result>;

public class AddCreditHandler(IAreliaDbContext context)
    : IRequestHandler<AddCreditCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        AddCreditCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount == 0)
            return Domain.Common.Result.Failure("Amount must not be zero.");

        context.CreditTransactions.Add(new CreditTransaction
        {
            PersonId = request.PersonId,
            Amount = request.Amount,
            Reason = request.Reason,
            PaymentId = request.PaymentId,
            ChargeId = request.ChargeId,
            Timestamp = DateTime.UtcNow,
            OrganizationId = request.OrganizationId,
        });

        // Update or create denormalized balance
        var balance = await context.CreditBalances
            .FirstOrDefaultAsync(b => b.PersonId == request.PersonId, cancellationToken);

        if (balance is null)
        {
            balance = new CreditBalance
            {
                PersonId = request.PersonId,
                BalanceAmount = request.Amount,
                CurrencyCode = (await context.Organizations
                    .FirstAsync(o => o.Id == request.OrganizationId, cancellationToken))
                    .DefaultCurrencyCode,
                OrganizationId = request.OrganizationId,
            };
            context.CreditBalances.Add(balance);
        }
        else
        {
            balance.BalanceAmount += request.Amount;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
