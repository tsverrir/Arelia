using Arelia.Application.Interfaces;
using Arelia.Domain.Common;
using Arelia.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.ExpenseCategories.Commands;

public record CreateExpenseCategoryCommand(string Name, Guid OrganizationId) : IRequest<Result<Guid>>;

public class CreateExpenseCategoryHandler(IAreliaDbContext context)
    : IRequestHandler<CreateExpenseCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<Guid>("Name is required.");

        var normalizedName = ExpenseCategory.Normalize(request.Name);

        var exists = await context.ExpenseCategories
            .IgnoreQueryFilters()
            .AnyAsync(c => c.OrganizationId == request.OrganizationId
                           && c.Name == normalizedName && c.IsActive, cancellationToken);

        if (exists)
            return Result.Failure<Guid>($"A category named '{request.Name}' already exists.");

        var category = new ExpenseCategory
        {
            Name = normalizedName,
            OrganizationId = request.OrganizationId,
        };

        context.ExpenseCategories.Add(category);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(category.Id);
    }
}
