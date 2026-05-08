using Arelia.Domain.Common;

namespace Arelia.Application.Finance.Commands;

public record SoftDeleteExpenseCommand(Guid ExpenseId) : IRequest<Result>;

/// <summary>
/// Soft-deletes an expense by marking it inactive.
/// A deletion record is created to preserve the audit trail.
/// </summary>
public class SoftDeleteExpenseHandler(IAreliaDbContext context)
    : IRequestHandler<SoftDeleteExpenseCommand, Result>
{
    public async Task<Result> Handle(
        SoftDeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var existing = await context.Expenses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId && e.IsActive, cancellationToken);

        if (existing is null)
            return Result.Failure("Expense not found.");

        existing.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
