using Arelia.Domain.Entities;

namespace Arelia.Application.Finance.Commands;

public record RecordPaymentCommand(
    Guid? ChargeId,
    Guid? PayerPersonId,
    string PayerDescription,
    decimal Amount,
    DateTime PaymentDate,
    string? PaymentMethod,
    string? Reference,
    string CurrencyCode,
    decimal? OriginalAmount,
    string? OriginalCurrencyCode,
    Guid OrganizationId) : IRequest<Domain.Common.Result<Guid>>;

public class RecordPaymentHandler(IAreliaDbContext context)
    : IRequestHandler<RecordPaymentCommand, Domain.Common.Result<Guid>>
{
    public async Task<Domain.Common.Result<Guid>> Handle(
        RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return Domain.Common.Result.Failure<Guid>("Amount must be greater than zero.");

        Charge? charge = null;
        Guid? payerPersonId = request.PayerPersonId;
        var payerDescription = request.PayerDescription?.Trim() ?? string.Empty;
        decimal overpaymentAmount = 0;

        if (request.ChargeId.HasValue)
        {
            charge = await context.Charges
                .Include(c => c.ChargeLines)
                .Include(c => c.Payments)
                .Include(c => c.Person)
                .FirstOrDefaultAsync(c =>
                    c.Id == request.ChargeId &&
                    c.OrganizationId == request.OrganizationId,
                    cancellationToken);

            if (charge is null)
                return Domain.Common.Result.Failure<Guid>("Charge not found.");

            if (!string.Equals(charge.CurrencyCode, request.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                return Domain.Common.Result.Failure<Guid>("Payment currency must match charge currency.");

            payerPersonId ??= charge.PersonId;
            if (string.IsNullOrWhiteSpace(payerDescription))
                payerDescription = charge.Person.FullName;

            var outstanding = Math.Max(0, charge.TotalDue - charge.TotalPaid);
            overpaymentAmount = Math.Max(0, request.Amount - outstanding);
        }

        if (payerPersonId is null && string.IsNullOrWhiteSpace(payerDescription))
            return Domain.Common.Result.Failure<Guid>("Either PayerPersonId or PayerDescription is required.");

        var payment = new Payment
        {
            ChargeId = request.ChargeId,
            PayerPersonId = payerPersonId,
            PayerDescription = payerDescription,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            PaymentMethod = request.PaymentMethod,
            Reference = request.Reference,
            CurrencyCode = request.CurrencyCode,
            OriginalAmount = request.OriginalAmount,
            OriginalCurrencyCode = request.OriginalCurrencyCode,
            OrganizationId = request.OrganizationId,
        };

        context.Payments.Add(payment);

        if (charge is not null)
        {
            if (!charge.Payments.Any(p => p.Id == payment.Id))
                charge.Payments.Add(payment);

            charge.RecalculateStatus();

            if (overpaymentAmount > 0 && payerPersonId is Guid personId)
            {
                context.CreditTransactions.Add(new CreditTransaction
                {
                    PersonId = personId,
                    Amount = overpaymentAmount,
                    Reason = $"Overpayment on {charge.Description}",
                    PaymentId = payment.Id,
                    ChargeId = charge.Id,
                    Timestamp = DateTime.UtcNow,
                    OrganizationId = request.OrganizationId,
                });

                var balance = await context.CreditBalances
                    .FirstOrDefaultAsync(b =>
                        b.PersonId == personId &&
                        b.OrganizationId == request.OrganizationId,
                        cancellationToken);

                if (balance is null)
                {
                    context.CreditBalances.Add(new CreditBalance
                    {
                        PersonId = personId,
                        BalanceAmount = overpaymentAmount,
                        CurrencyCode = request.CurrencyCode,
                        OrganizationId = request.OrganizationId,
                    });
                }
                else
                {
                    balance.BalanceAmount += overpaymentAmount;
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success(payment.Id);
    }
}
