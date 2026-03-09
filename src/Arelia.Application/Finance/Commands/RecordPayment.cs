using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        if (request.PayerPersonId is null && string.IsNullOrWhiteSpace(request.PayerDescription))
            return Domain.Common.Result.Failure<Guid>("Either PayerPersonId or PayerDescription is required.");

        var payment = new Payment
        {
            ChargeId = request.ChargeId,
            PayerPersonId = request.PayerPersonId,
            PayerDescription = request.PayerDescription,
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

        // Update charge status if linked
        if (request.ChargeId.HasValue)
        {
            var charge = await context.Charges
                .Include(c => c.ChargeLines)
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(c => c.Id == request.ChargeId, cancellationToken);

            if (charge is not null)
            {
                // Ensure the new payment is in the collection for recalculation
                if (!charge.Payments.Any(p => p.Id == payment.Id))
                    charge.Payments.Add(payment);

                charge.RecalculateStatus();
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success(payment.Id);
    }
}
