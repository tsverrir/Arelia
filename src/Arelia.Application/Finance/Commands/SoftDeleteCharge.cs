using Arelia.Domain.Common;

namespace Arelia.Application.Finance.Commands;

public record SoftDeleteChargeCommand(Guid ChargeId) : IRequest<Result>;

/// <summary>
/// Soft-deletes a charge by marking it inactive.
/// </summary>
public class SoftDeleteChargeHandler(IAreliaDbContext context)
    : IRequestHandler<SoftDeleteChargeCommand, Result>
{
    public async Task<Result> Handle(
        SoftDeleteChargeCommand request, CancellationToken cancellationToken)
    {
        var existing = await context.Charges
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.ChargeId && c.IsActive, cancellationToken);

        if (existing is null)
            return Result.Failure("Charge not found.");

        existing.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
