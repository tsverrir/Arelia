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
            FromDate = DateTime.UtcNow.AddDays(-10),
            ToDate = DateTime.UtcNow.AddDays(10)
        };

        assignment.IsCurrentlyActive.Should().BeTrue();
    }

    [Fact]
    public void WhenBeforeFromDateThenIsCurrentlyActiveShouldBeFalse()
    {
        var assignment = new RoleAssignment
        {
            FromDate = DateTime.UtcNow.AddDays(5),
            ToDate = DateTime.UtcNow.AddDays(10)
        };

        assignment.IsCurrentlyActive.Should().BeFalse();
    }

    [Fact]
    public void WhenAfterToDateThenIsCurrentlyActiveShouldBeFalse()
    {
        var assignment = new RoleAssignment
        {
            FromDate = DateTime.UtcNow.AddDays(-20),
            ToDate = DateTime.UtcNow.AddDays(-5)
        };

        assignment.IsCurrentlyActive.Should().BeFalse();
    }

    [Fact]
    public void WhenNoEndDateThenIsCurrentlyActiveShouldBeTrue()
    {
        var assignment = new RoleAssignment
        {
            FromDate = DateTime.UtcNow.AddDays(-10),
            ToDate = null
        };

        assignment.IsCurrentlyActive.Should().BeTrue();
    }
}
