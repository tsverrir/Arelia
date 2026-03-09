using Arelia.Application.Activities.Commands;
using Arelia.Application.Rehearsals.Commands;
using Arelia.Application.Tests;
using Arelia.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Tests.Rehearsals;

public class GenerateRehearsalsTests
{
    [Fact]
    public async Task WhenGeneratingThenRehearsalsAreCreatedForMatchingDays()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        // Create semester: Jan 6 - Feb 28, 2025 (covers multiple Thursdays)
        var actHandler = new CreateActivityHandler(context);
        var semResult = await actHandler.Handle(new CreateActivityCommand(
            "Spring 2025", null, ActivityType.Semester,
            new DateTime(2025, 1, 6), new DateTime(2025, 2, 28),
            null, null, false, null, null, orgId), CancellationToken.None);

        // Create template: Every Thursday
        var templateHandler = new CreateRehearsalTemplateHandler(context);
        await templateHandler.Handle(new CreateRehearsalTemplateCommand(
            semResult.Value!, DayOfWeek.Thursday, new TimeOnly(19, 0), 150,
            "Rehearsal Hall", new DateTime(2025, 1, 6), new DateTime(2025, 2, 28), orgId),
            CancellationToken.None);

        // Generate
        var genHandler = new GenerateRehearsalsHandler(context);
        var result = await genHandler.Handle(
            new GenerateRehearsalsCommand(semResult.Value!, orgId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Thursdays in Jan 6 - Feb 28: Jan 9, 16, 23, 30, Feb 6, 13, 20, 27 = 8
        result.Value!.Created.Should().Be(8);
    }

    [Fact]
    public async Task WhenRunTwiceThenSecondRunSkipsExisting()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        var actHandler = new CreateActivityHandler(context);
        var semResult = await actHandler.Handle(new CreateActivityCommand(
            "Spring 2025", null, ActivityType.Semester,
            new DateTime(2025, 1, 6), new DateTime(2025, 1, 31),
            null, null, false, null, null, orgId), CancellationToken.None);

        var templateHandler = new CreateRehearsalTemplateHandler(context);
        await templateHandler.Handle(new CreateRehearsalTemplateCommand(
            semResult.Value!, DayOfWeek.Thursday, new TimeOnly(19, 0), 150,
            null, new DateTime(2025, 1, 6), new DateTime(2025, 1, 31), orgId),
            CancellationToken.None);

        var genHandler = new GenerateRehearsalsHandler(context);

        // First run
        var r1 = await genHandler.Handle(
            new GenerateRehearsalsCommand(semResult.Value!, orgId), CancellationToken.None);

        // Second run — should all be skipped
        var r2 = await genHandler.Handle(
            new GenerateRehearsalsCommand(semResult.Value!, orgId), CancellationToken.None);

        r1.Value!.Created.Should().BeGreaterThan(0);
        r2.Value!.Created.Should().Be(0);
        r2.Value!.Skipped.Should().Be(r1.Value!.Created);
    }

    [Fact]
    public async Task WhenTemplateDatesOutsideSemesterThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);

        var actHandler = new CreateActivityHandler(context);
        var semResult = await actHandler.Handle(new CreateActivityCommand(
            "Spring 2025", null, ActivityType.Semester,
            new DateTime(2025, 1, 6), new DateTime(2025, 6, 30),
            null, null, false, null, null, orgId), CancellationToken.None);

        var templateHandler = new CreateRehearsalTemplateHandler(context);
        var result = await templateHandler.Handle(new CreateRehearsalTemplateCommand(
            semResult.Value!, DayOfWeek.Thursday, new TimeOnly(19, 0), 150,
            null, new DateTime(2024, 12, 1), new DateTime(2025, 6, 30), orgId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("within the semester");
    }
}
