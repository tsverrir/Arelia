using Arelia.Application.Finance.Commands;
using Arelia.Application.Tests;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Tests.Finance;

public class FinanceTests
{
    [Fact]
    public async Task WhenGeneratingFeesThenOneChargePerActiveMember()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        context.Organizations.Add(new Organization { Id = orgId, Name = "Choir", DefaultCurrencyCode = "DKK" });
        context.Persons.Add(new Person { FirstName = "A", LastName = "One", OrganizationId = orgId });
        context.Persons.Add(new Person { FirstName = "B", LastName = "Two", OrganizationId = orgId });
        context.Persons.Add(new Person { FirstName = "C", LastName = "Inactive", OrganizationId = orgId, IsActive = false });
        var semesterId = Guid.NewGuid();
        context.Activities.Add(new Activity
        {
            Id = semesterId,
            Name = "Spring 2026",
            ActivityType = ActivityType.Semester,
            StartDateTime = new DateTime(2026, 1, 1),
            EndDateTime = new DateTime(2026, 6, 30),
            OrganizationId = orgId
        });
        await context.SaveChangesAsync();

        var handler = new GenerateMembershipFeesHandler(context);
        var result = await handler.Handle(new GenerateMembershipFeesCommand(
            semesterId, 500m, 100m, DateTime.UtcNow.AddDays(30), orgId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        var charges = await context.Charges.IgnoreQueryFilters()
            .Include(c => c.ChargeLines)
            .Where(c => c.OrganizationId == orgId)
            .ToListAsync();

        charges.Should().HaveCount(2);
        charges.Should().OnlyContain(c => c.ChargeLines.Count == 2);
        charges.Should().OnlyContain(c => c.CurrencyCode == "DKK");

        var firstCharge = charges.First();
        firstCharge.ChargeLines.Should().Contain(cl => cl.LineType == ChargeLineType.Base && cl.IsSelected);
        firstCharge.ChargeLines.Should().Contain(cl => cl.LineType == ChargeLineType.TopUp && !cl.IsSelected);
    }

    [Fact]
    public async Task WhenRecordingPaymentThenChargeStatusUpdates()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        var personId = Guid.NewGuid();
        context.Persons.Add(new Person { Id = personId, FirstName = "Test", LastName = "User", OrganizationId = orgId });

        var charge = new Charge
        {
            PersonId = personId,
            Description = "Test fee",
            DueDate = DateTime.UtcNow.AddDays(30),
            CurrencyCode = "DKK",
            OrganizationId = orgId,
        };
        charge.ChargeLines.Add(new ChargeLine
        {
            Amount = 500m,
            Description = "Base",
            LineType = ChargeLineType.Base,
            IsSelected = true,
            OrganizationId = orgId,
        });
        context.Charges.Add(charge);
        await context.SaveChangesAsync();

        var handler = new RecordPaymentHandler(context);
        await handler.Handle(new RecordPaymentCommand(
            charge.Id, personId, "Test User", 500m, DateTime.UtcNow,
            "MobilePay", "REF-1", "DKK", null, null, orgId),
            CancellationToken.None);

        var updated = await context.Charges.IgnoreQueryFilters()
            .FirstAsync(c => c.Id == charge.Id);
        updated.Status.Should().Be(ChargeStatus.Paid);
    }

    [Fact]
    public async Task WhenPayerMissingThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        var handler = new RecordPaymentHandler(context);
        var result = await handler.Handle(new RecordPaymentCommand(
            null, null, "", 100m, DateTime.UtcNow,
            null, null, "DKK", null, null, orgId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("PayerPersonId or PayerDescription");
    }
}
