using Arelia.Application.Interfaces;
using Arelia.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.ExpenseCategories.Commands;

public record DeleteExpenseCategoryCommand(Guid CategoryId, Guid? MoveToCategoryId = null) : IRequest<Result>;

public class DeleteExpenseCategoryHandler(IAreliaDbContext context)
    : IRequestHandler<DeleteExpenseCategoryCommand, Result>
{
    public async Task<Result> Handle(
        DeleteExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.ExpenseCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
            return Result.Failure("Category not found.");

        var assignedExpenses = await context.Expenses
            .IgnoreQueryFilters()
            .Where(e => e.CategoryId == request.CategoryId && e.IsActive)
            .ToListAsync(cancellationToken);

        if (assignedExpenses.Count > 0)
        {
            if (request.MoveToCategoryId is not Guid targetId)
                return Result.Failure("A target category must be specified when expenses exist.");

            var targetExists = await context.ExpenseCategories
                .IgnoreQueryFilters()
                .AnyAsync(c => c.Id == targetId && c.IsActive, cancellationToken);

            if (!targetExists)
                return Result.Failure("Target category not found or is inactive.");

            foreach (var expense in assignedExpenses)
                expense.CategoryId = targetId;
        }

        category.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
