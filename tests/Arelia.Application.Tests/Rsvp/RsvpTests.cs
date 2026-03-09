using Arelia.Application.Rsvp.Commands;
using Arelia.Application.Tests;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Tests.Rsvp;

public class RsvpTests
{
    [Fact]
    public async Task WhenRsvpNoForRehearsalThenAbsenceRecordIsCreated()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);
        var personId = Guid.NewGuid();

        context.Activities.Add(new Activity
        {
            Id = Guid.NewGuid(),
            Name = "Rehearsal",
            ActivityType = ActivityType.Rehearsal,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(2),
            OrganizationId = orgId,
        });
        context.Persons.Add(new Person { Id = personId, FirstName = "Test", LastName = "User", OrganizationId = orgId });
        await context.SaveChangesAsync();

        var activityId = (await context.Activities.FirstAsync()).Id;
        var handler = new RsvpHandler(context);

        var result = await handler.Handle(
            new RsvpCommand(activityId, personId, RsvpStatus.No, orgId), CancellationToken.None);
        await context.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();
        var participant = await context.ActivityParticipants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.ActivityId == activityId && p.PersonId == personId);
        participant.Should().NotBeNull();
        participant!.RsvpStatus.Should().Be(RsvpStatus.No);
    }

    [Fact]
    public async Task WhenRsvpYesForRehearsalThenAbsenceRecordIsDeactivated()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);
        var personId = Guid.NewGuid();
        var activityId = Guid.NewGuid();

        context.Activities.Add(new Activity
        {
            Id = activityId,
            Name = "Rehearsal",
            ActivityType = ActivityType.Rehearsal,
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(2),
            OrganizationId = orgId,
        });
        context.Persons.Add(new Person { Id = personId, FirstName = "Test", LastName = "User", OrganizationId = orgId });
        context.ActivityParticipants.Add(new ActivityParticipant
        {
            ActivityId = activityId,
            PersonId = personId,
            RsvpStatus = RsvpStatus.No,
            OrganizationId = orgId,
        });
        await context.SaveChangesAsync();

        var handler = new RsvpHandler(context);
        var result = await handler.Handle(
            new RsvpCommand(activityId, personId, RsvpStatus.Yes, orgId), CancellationToken.None);
        await context.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();
        var participant = await context.ActivityParticipants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.ActivityId == activityId && p.PersonId == personId);
        participant!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task WhenCapacityFullThenWaitlisted()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);
        var activityId = Guid.NewGuid();
        var person1 = Guid.NewGuid();
        var person2 = Guid.NewGuid();

        context.Activities.Add(new Activity
        {
            Id = activityId,
            Name = "Concert",
            ActivityType = ActivityType.Concert,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2),
            MaxCapacity = 1,
            OrganizationId = orgId,
        });
        context.Persons.Add(new Person { Id = person1, FirstName = "First", LastName = "User", OrganizationId = orgId });
        context.Persons.Add(new Person { Id = person2, FirstName = "Second", LastName = "User", OrganizationId = orgId });
        await context.SaveChangesAsync();

        var handler = new RsvpHandler(context);

        // First person gets confirmed
        await handler.Handle(new RsvpCommand(activityId, person1, RsvpStatus.Yes, orgId), CancellationToken.None);
        await context.SaveChangesAsync();

        // Second person gets waitlisted
        await handler.Handle(new RsvpCommand(activityId, person2, RsvpStatus.Yes, orgId), CancellationToken.None);
        await context.SaveChangesAsync();

        var p1 = await context.ActivityParticipants.IgnoreQueryFilters()
            .FirstAsync(p => p.PersonId == person1 && p.ActivityId == activityId);
        var p2 = await context.ActivityParticipants.IgnoreQueryFilters()
            .FirstAsync(p => p.PersonId == person2 && p.ActivityId == activityId);

        p1.SignupStatus.Should().Be(SignupStatus.Confirmed);
        p2.SignupStatus.Should().Be(SignupStatus.Waitlisted);
        p2.WaitlistPosition.Should().Be(1);
    }

    [Fact]
    public async Task WhenPastDeadlineThenReturnsFailure()
    {
        var orgId = Guid.NewGuid();
        await using var context = TestDbContextFactory.Create(orgId);
        var activityId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        context.Activities.Add(new Activity
        {
            Id = activityId,
            Name = "Concert",
            ActivityType = ActivityType.Concert,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2),
            SignupDeadline = DateTime.UtcNow.AddDays(-1),
            OrganizationId = orgId,
        });
        context.Persons.Add(new Person { Id = personId, FirstName = "Test", LastName = "User", OrganizationId = orgId });
        await context.SaveChangesAsync();

        var handler = new RsvpHandler(context);
        var result = await handler.Handle(
            new RsvpCommand(activityId, personId, RsvpStatus.Yes, orgId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("deadline");
    }
}
