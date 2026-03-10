using Arelia.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.ExpenseCategories.Queries;

public record GetExpenseCategoriesQuery(Guid OrganizationId) : IRequest<List<ExpenseCategoryDto>>;

public record ExpenseCategoryDto(Guid Id, string Name, int ExpenseCount);

public class GetExpenseCategoriesHandler(IAreliaDbContext context)
    : IRequestHandler<GetExpenseCategoriesQuery, List<ExpenseCategoryDto>>
{
    public async Task<List<ExpenseCategoryDto>> Handle(
        GetExpenseCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await context.ExpenseCategories
            .IgnoreQueryFilters()
            .Where(c => c.OrganizationId == request.OrganizationId && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new ExpenseCategoryDto(
                c.Id,
                c.Name,
                c.Expenses.Count(e => e.IsActive)))
            .ToListAsync(cancellationToken);
    }
}
