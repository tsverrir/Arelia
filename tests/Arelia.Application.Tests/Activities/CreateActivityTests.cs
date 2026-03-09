using Arelia.Application.Activities.Commands;
using Arelia.Application.Tests;
using Arelia.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Tests.Activities;

public class CreateActivityTests
{
    [Fact]
    public async Task WhenCreatingActivityThenWorkYearIsComputed()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);
        var handler = new CreateActivityHandler(context);

        var result = await handler.Handle(new CreateActivityCommand(
            "Spring Concert", null, ActivityType.Concert,
            new DateTime(2025, 3, 15, 19, 0, 0), new DateTime(2025, 3, 15, 21, 0, 0),
            "Concert Hall", null, false, null, null, orgId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var activity = await context.Activities.IgnoreQueryFilters().FirstAsync(a => a.Id == result.Value);
        activity.WorkYear.Should().Be(2025);
    }

    [Fact]
    public async Task WhenSemestersOverlapThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);
        var handler = new CreateActivityHandler(context);

        // Create first semester
        await handler.Handle(new CreateActivityCommand(
            "Fall 2025", null, ActivityType.Semester,
            new DateTime(2025, 8, 1), new DateTime(2025, 12, 31),
            null, null, false, null, null, orgId), CancellationToken.None);

        // Overlapping semester
        var result = await handler.Handle(new CreateActivityCommand(
            "Overlapping", null, ActivityType.Semester,
            new DateTime(2025, 11, 1), new DateTime(2026, 1, 31),
            null, null, false, null, null, orgId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("overlap");
    }

    [Fact]
    public async Task WhenNestingMoreThanOneLevelThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);
        var handler = new CreateActivityHandler(context);

        // Semester
        var semesterResult = await handler.Handle(new CreateActivityCommand(
            "Fall 2025", null, ActivityType.Semester,
            new DateTime(2025, 8, 1), new DateTime(2025, 12, 31),
            null, null, false, null, null, orgId), CancellationToken.None);

        // Activity under semester (OK)
        var childResult = await handler.Handle(new CreateActivityCommand(
            "Concert", null, ActivityType.Concert,
            new DateTime(2025, 10, 1, 19, 0, 0), new DateTime(2025, 10, 1, 21, 0, 0),
            null, semesterResult.Value, false, null, null, orgId), CancellationToken.None);

        childResult.IsSuccess.Should().BeTrue();

        // Activity under child (should fail)
        var grandchildResult = await handler.Handle(new CreateActivityCommand(
            "Sub-event", null, ActivityType.Other,
            new DateTime(2025, 10, 1, 19, 0, 0), new DateTime(2025, 10, 1, 20, 0, 0),
            null, childResult.Value, false, null, null, orgId), CancellationToken.None);

        grandchildResult.IsFailure.Should().BeTrue();
        grandchildResult.Error.Should().Contain("one level");
    }

    [Fact]
    public async Task WhenNonOverlappingSemestersThenBothSucceed()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);
        var handler = new CreateActivityHandler(context);

        var r1 = await handler.Handle(new CreateActivityCommand(
            "Spring 2025", null, ActivityType.Semester,
            new DateTime(2025, 1, 1), new DateTime(2025, 6, 30),
            null, null, false, null, null, orgId), CancellationToken.None);

        var r2 = await handler.Handle(new CreateActivityCommand(
            "Fall 2025", null, ActivityType.Semester,
            new DateTime(2025, 8, 1), new DateTime(2025, 12, 31),
            null, null, false, null, null, orgId), CancellationToken.None);

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();
    }
}
