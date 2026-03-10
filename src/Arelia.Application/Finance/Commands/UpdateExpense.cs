using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Finance.Commands;

public record UpdateExpenseCommand(
    Guid ExpenseId,
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    Guid CategoryId,
    string? ReceivedBy,
    string? Notes) : IRequest<Domain.Common.Result<Guid>>;

/// <summary>
/// Creates a new revision of the expense and marks the old one as inactive.
/// </summary>
public class UpdateExpenseHandler(IAreliaDbContext context)
    : IRequestHandler<UpdateExpenseCommand, Domain.Common.Result<Guid>>
{
    public async Task<Domain.Common.Result<Guid>> Handle(
        UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var existing = await context.Expenses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId && e.IsActive, cancellationToken);

        if (existing is null)
            return Domain.Common.Result.Failure<Guid>("Expense not found.");

        var categoryExists = await context.ExpenseCategories
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken);

        if (!categoryExists)
            return Domain.Common.Result.Failure<Guid>("Category not found.");

        var revision = new Expense
        {
            Description = request.Description,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            CategoryId = request.CategoryId,
            CurrencyCode = existing.CurrencyCode,
            ReceivedBy = request.ReceivedBy,
            Notes = request.Notes,
            OrganizationId = existing.OrganizationId,
            OriginalId = existing.OriginalId ?? existing.Id,
        };

        existing.IsActive = false;
        existing.ReplacedById = revision.Id;

        context.Expenses.Add(revision);
        await context.SaveChangesAsync(cancellationToken);

        return Domain.Common.Result.Success(revision.Id);
    }
}
