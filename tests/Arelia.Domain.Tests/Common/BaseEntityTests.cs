using Arelia.Domain.Common;
using Arelia.Domain.Entities;
using FluentAssertions;

namespace Arelia.Domain.Tests.Common;

public class BaseEntityTests
{
    [Fact]
    public void NewEntityShouldHaveGeneratedId()
    {
        var entity = new Person();

        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void NewEntityShouldBeActive()
    {
        var entity = new Person();

        entity.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ShouldAddDomainEvent()
    {
        var entity = new Person();
        var domainEvent = new TestDomainEvent();

        entity.AddDomainEvent(domainEvent);

        entity.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(domainEvent);
    }

    [Fact]
    public void ShouldRemoveDomainEvent()
    {
        var entity = new Person();
        var domainEvent = new TestDomainEvent();
        entity.AddDomainEvent(domainEvent);

        entity.RemoveDomainEvent(domainEvent);

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ShouldClearAllDomainEvents()
    {
        var entity = new Person();
        entity.AddDomainEvent(new TestDomainEvent());
        entity.AddDomainEvent(new TestDomainEvent());

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }

    private class TestDomainEvent : DomainEvent;
}
