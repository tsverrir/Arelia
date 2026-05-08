using Arelia.Domain.Entities;

namespace Arelia.Application.Finance.Commands;

public record RecordExpenseCommand(
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    string Category,
    string? ReceivedBy,
    string? Notes,
    Guid OrganizationId) : IRequest<Domain.Common.Result<Guid>>;

public class RecordExpenseHandler(IAreliaDbContext context)
    : IRequestHandler<RecordExpenseCommand, Domain.Common.Result<Guid>>
{
    public async Task<Domain.Common.Result<Guid>> Handle(
        RecordExpenseCommand request, CancellationToken cancellationToken)
    {
        var normalizedCategory = ExpenseCategory.Normalize(request.Category);

        if (string.IsNullOrEmpty(normalizedCategory))
            return Domain.Common.Result.Failure<Guid>("Category is required.");

        // Ensure category exists
        var category = await context.ExpenseCategories
            .FirstOrDefaultAsync(c =>
                c.OrganizationId == request.OrganizationId &&
                c.Name == normalizedCategory,
                cancellationToken);

        if (category is null)
        {
            category = new ExpenseCategory
            {
                Name = normalizedCategory,
                OrganizationId = request.OrganizationId,
            };
            context.ExpenseCategories.Add(category);
        }

        var org = await context.Organizations
            .FirstAsync(o => o.Id == request.OrganizationId, cancellationToken);

        var expense = new Expense
        {
            Description = request.Description,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            CategoryId = category.Id,
            CurrencyCode = org.DefaultCurrencyCode,
            ReceivedBy = request.ReceivedBy,
            Notes = request.Notes,
            OrganizationId = request.OrganizationId,
        };

        context.Expenses.Add(expense);
        await context.SaveChangesAsync(cancellationToken);

        return Domain.Common.Result.Success(expense.Id);
    }
}
