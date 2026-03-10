using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Finance.Commands;

public record UpdateChargeCommand(
    Guid ChargeId,
    string Description,
    DateTime DueDate) : IRequest<Domain.Common.Result<Guid>>;

/// <summary>
/// Creates a new revision of the charge (with its charge lines) and marks the old one as inactive.
/// Payments remain linked to the old charge for traceability.
/// </summary>
public class UpdateChargeHandler(IAreliaDbContext context)
    : IRequestHandler<UpdateChargeCommand, Domain.Common.Result<Guid>>
{
    public async Task<Domain.Common.Result<Guid>> Handle(
        UpdateChargeCommand request, CancellationToken cancellationToken)
    {
        var existing = await context.Charges
            .IgnoreQueryFilters()
            .Include(c => c.ChargeLines)
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == request.ChargeId && c.IsActive, cancellationToken);

        if (existing is null)
            return Domain.Common.Result.Failure<Guid>("Charge not found.");

        var revision = new Charge
        {
            PersonId = existing.PersonId,
            SemesterId = existing.SemesterId,
            Description = request.Description,
            DueDate = request.DueDate,
            CurrencyCode = existing.CurrencyCode,
            OrganizationId = existing.OrganizationId,
            OriginalId = existing.OriginalId ?? existing.Id,
        };

        // Copy active charge lines to the new revision
        foreach (var line in existing.ChargeLines.Where(cl => cl.IsActive))
        {
            revision.ChargeLines.Add(new ChargeLine
            {
                Description = line.Description,
                Amount = line.Amount,
                LineType = line.LineType,
                IsSelected = line.IsSelected,
                OrganizationId = existing.OrganizationId,
            });
        }

        // Move active payments to the new revision
        foreach (var payment in existing.Payments.Where(p => p.IsActive))
        {
            payment.ChargeId = revision.Id;
        }

        revision.RecalculateStatus();

        existing.IsActive = false;
        existing.ReplacedById = revision.Id;

        context.Charges.Add(revision);
        await context.SaveChangesAsync(cancellationToken);

        return Domain.Common.Result.Success(revision.Id);
    }
}
