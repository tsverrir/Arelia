using Arelia.Domain.Entities;
using FluentAssertions;

namespace Arelia.Domain.Tests.Entities;

public class RoleAssignmentTests
{
    [Fact]
    public void WhenWithinDateRangeThenIsCurrentlyActiveShouldBeTrue()
    {
        var assignment = new RoleAssignment
        {
            FromDate = DateTime.Today.AddDays(-10),
            ToDate = DateTime.Today.AddDays(10)
        };

        assignment.IsCurrentlyActive.Should().BeTrue();
    }

    [Fact]
    public void WhenBeforeFromDateThenIsCurrentlyActiveShouldBeFalse()
    {
        var assignment = new RoleAssignment
        {
            FromDate = DateTime.Today.AddDays(5),
            ToDate = DateTime.Today.AddDays(10)
        };

        assignment.IsCurrentlyActive.Should().BeFalse();
    }

    [Fact]
    public void WhenAfterToDateThenIsCurrentlyActiveShouldBeFalse()
    {
        var assignment = new RoleAssignment
        {
            FromDate = DateTime.Today.AddDays(-20),
            ToDate = DateTime.Today.AddDays(-5)
        };

        assignment.IsCurrentlyActive.Should().BeFalse();
    }

    [Fact]
    public void WhenNoEndDateThenIsCurrentlyActiveShouldBeTrue()
    {
        var assignment = new RoleAssignment
        {
            FromDate = DateTime.Today.AddDays(-10),
            ToDate = null
        };

        assignment.IsCurrentlyActive.Should().BeTrue();
    }

    [Fact]
    public void WhenToDateIsEndedTodayThenIsCurrentlyActiveShouldBeFalse()
    {
        // "End Now" sets ToDate = DateTime.Today — role should be immediately inactive
        var assignment = new RoleAssignment
        {
            FromDate = DateTime.Today.AddDays(-10),
            ToDate = DateTime.Today
        };

        assignment.IsCurrentlyActive.Should().BeFalse();
    }

    [Fact]
    public void WhenToDateIsStartingTodayThenIsCurrentlyActiveShouldBeTrue()
    {
        var assignment = new RoleAssignment
        {
            FromDate = DateTime.Today,
            ToDate = DateTime.Today.AddDays(30)
        };

        assignment.IsCurrentlyActive.Should().BeTrue();
    }
}
