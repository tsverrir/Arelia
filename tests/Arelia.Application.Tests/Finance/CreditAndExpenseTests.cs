using Arelia.Application.Finance.Commands;
using Arelia.Application.Tests;
using Arelia.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Tests.Finance;

public class CreditAndExpenseTests
{
    [Fact]
    public async Task WhenAddingCreditThenBalanceIsCreated()
    {
        var orgId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        context.Organizations.Add(new Organization { Id = orgId, Name = "Choir", DefaultCurrencyCode = "DKK" });
        context.Persons.Add(new Person { Id = personId, FirstName = "A", LastName = "B", OrganizationId = orgId });
        await context.SaveChangesAsync();

        var handler = new AddCreditHandler(context);
        await handler.Handle(new AddCreditCommand(personId, 200m, "Overpayment refund", null, null, orgId),
            CancellationToken.None);

        var balance = await context.CreditBalances.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.PersonId == personId);
        balance.Should().NotBeNull();
        balance!.BalanceAmount.Should().Be(200m);
    }

    [Fact]
    public async Task WhenAddingMultipleCreditsThenBalanceAccumulates()
    {
        var orgId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        context.Organizations.Add(new Organization { Id = orgId, Name = "Choir", DefaultCurrencyCode = "DKK" });
        context.Persons.Add(new Person { Id = personId, FirstName = "A", LastName = "B", OrganizationId = orgId });
        await context.SaveChangesAsync();

        var handler = new AddCreditHandler(context);
        await handler.Handle(new AddCreditCommand(personId, 100m, "First", null, null, orgId), CancellationToken.None);
        await handler.Handle(new AddCreditCommand(personId, -30m, "Usage", null, null, orgId), CancellationToken.None);

        var balance = await context.CreditBalances.IgnoreQueryFilters()
            .FirstAsync(b => b.PersonId == personId);
        balance.BalanceAmount.Should().Be(70m);

        var transactions = await context.CreditTransactions.IgnoreQueryFilters()
            .Where(t => t.PersonId == personId)
            .ToListAsync();
        transactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task WhenZeroAmountThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        var handler = new AddCreditHandler(context);
        var result = await handler.Handle(new AddCreditCommand(Guid.NewGuid(), 0m, "Oops", null, null, orgId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task WhenRecordingExpenseThenCategoryIsAutoCreated()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        context.Organizations.Add(new Organization { Id = orgId, Name = "Choir", DefaultCurrencyCode = "DKK" });
        await context.SaveChangesAsync();

        var handler = new RecordExpenseHandler(context);
        var result = await handler.Handle(new RecordExpenseCommand(
            "New piano tuning", 2500m, DateTime.UtcNow, "piano maintenance", "John", null, orgId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var category = await context.ExpenseCategories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Name == "PIANO MAINTENANCE" && c.OrganizationId == orgId);
        category.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenRecordingExpenseWithExistingCategoryThenNoNewCategory()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        context.Organizations.Add(new Organization { Id = orgId, Name = "Choir", DefaultCurrencyCode = "DKK" });
        context.ExpenseCategories.Add(new ExpenseCategory { Name = "SHEET MUSIC", OrganizationId = orgId });
        await context.SaveChangesAsync();

        var handler = new RecordExpenseHandler(context);
        await handler.Handle(new RecordExpenseCommand(
            "Bach BWV 244", 350m, DateTime.UtcNow, "  sheet  music ", null, null, orgId),
            CancellationToken.None);

        var categories = await context.ExpenseCategories.IgnoreQueryFilters()
            .Where(c => c.OrganizationId == orgId)
            .ToListAsync();
        categories.Should().HaveCount(1);
    }
}
