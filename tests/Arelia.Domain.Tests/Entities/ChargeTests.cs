using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using FluentAssertions;

namespace Arelia.Domain.Tests.Entities;

public class ChargeTests
{
    [Fact]
    public void WhenNoPaymentsThenStatusShouldBeOpen()
    {
        var charge = CreateCharge(baseAmount: 100m);

        charge.RecalculateStatus();

        charge.Status.Should().Be(ChargeStatus.Open);
    }

    [Fact]
    public void WhenPartialPaymentThenStatusShouldBePartiallyPaid()
    {
        var charge = CreateCharge(baseAmount: 100m);
        charge.Payments.Add(new Payment { Amount = 50m, IsActive = true });

        charge.RecalculateStatus();

        charge.Status.Should().Be(ChargeStatus.PartiallyPaid);
    }

    [Fact]
    public void WhenFullPaymentThenStatusShouldBePaid()
    {
        var charge = CreateCharge(baseAmount: 100m);
        charge.Payments.Add(new Payment { Amount = 100m, IsActive = true });

        charge.RecalculateStatus();

        charge.Status.Should().Be(ChargeStatus.Paid);
    }

    [Fact]
    public void WhenOverpaidThenStatusShouldBeOverpaid()
    {
        var charge = CreateCharge(baseAmount: 100m);
        charge.Payments.Add(new Payment { Amount = 150m, IsActive = true });

        charge.RecalculateStatus();

        charge.Status.Should().Be(ChargeStatus.Overpaid);
    }

    [Fact]
    public void DeactivatedChargeLinesShouldNotCountTowardTotal()
    {
        var charge = CreateCharge(baseAmount: 100m);
        charge.ChargeLines.Add(new ChargeLine
        {
            Amount = 50m,
            LineType = ChargeLineType.TopUp,
            IsSelected = true,
            IsActive = false
        });
        charge.Payments.Add(new Payment { Amount = 100m, IsActive = true });

        charge.RecalculateStatus();

        charge.TotalDue.Should().Be(100m);
        charge.Status.Should().Be(ChargeStatus.Paid);
    }

    [Fact]
    public void UnselectedLinesShouldNotCountTowardTotal()
    {
        var charge = CreateCharge(baseAmount: 100m);
        charge.ChargeLines.Add(new ChargeLine
        {
            Amount = 50m,
            LineType = ChargeLineType.TopUp,
            IsSelected = false,
            IsActive = true
        });

        charge.TotalDue.Should().Be(100m);
    }

    [Fact]
    public void DiscountLineShouldReduceTotal()
    {
        var charge = CreateCharge(baseAmount: 100m);
        charge.ChargeLines.Add(new ChargeLine
        {
            Amount = -20m,
            LineType = ChargeLineType.Discount,
            IsSelected = true,
            IsActive = true
        });

        charge.TotalDue.Should().Be(80m);
    }

    private static Charge CreateCharge(decimal baseAmount) => new()
    {
        ChargeLines =
        [
            new ChargeLine
            {
                Amount = baseAmount,
                LineType = ChargeLineType.Base,
                IsSelected = true,
                IsActive = true
            }
        ],
        Payments = []
    };
}
