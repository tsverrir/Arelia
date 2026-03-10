using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.ExpenseCategories.Commands;

public record UpdateExpenseCategoryCommand(Guid Id, string Name) : IRequest<Domain.Common.Result>;

public class UpdateExpenseCategoryHandler(IAreliaDbContext context)
    : IRequestHandler<UpdateExpenseCategoryCommand, Domain.Common.Result>
{
    public async Task<Domain.Common.Result> Handle(
        UpdateExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Domain.Common.Result.Failure("Name is required.");

        var category = await context.ExpenseCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
            return Domain.Common.Result.Failure("Category not found.");

        var normalizedName = ExpenseCategory.Normalize(request.Name);

        var duplicate = await context.ExpenseCategories
            .IgnoreQueryFilters()
            .AnyAsync(c => c.OrganizationId == category.OrganizationId
                           && c.Name == normalizedName && c.IsActive && c.Id != request.Id, cancellationToken);

        if (duplicate)
            return Domain.Common.Result.Failure($"A category named '{request.Name}' already exists.");

        category.Name = normalizedName;

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success();
    }
}
