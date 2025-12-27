using VSA.Kernel;

namespace VSA.Kernel.Tests;

public class EntityTests
{
    private sealed record TestDomainEvent(string Message) : DomainEvent;

    private sealed class TestEntity : Entity
    {
        public void RaiseTestEvent(string message)
        {
            RaiseDomainEvent(new TestDomainEvent(message));
        }
    }

    [Fact]
    public void RaiseDomainEvent_ShouldAddEventToCollection()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.RaiseTestEvent("Test message");

        // Assert
        Assert.Single(entity.DomainEvents);
        Assert.IsType<TestDomainEvent>(entity.DomainEvents[0]);
    }

    [Fact]
    public void RaiseDomainEvent_MultipleTimes_ShouldAddAllEvents()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.RaiseTestEvent("First");
        entity.RaiseTestEvent("Second");
        entity.RaiseTestEvent("Third");

        // Assert
        Assert.Equal(3, entity.DomainEvents.Count);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var entity = new TestEntity();
        entity.RaiseTestEvent("Test");

        // Act
        entity.ClearDomainEvents();

        // Assert
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void GetDomainEvents_ShouldReturnReadOnlyList()
    {
        // Arrange
        var entity = new TestEntity();
        entity.RaiseTestEvent("Test");

        // Act
        var events = entity.GetDomainEvents();

        // Assert
        Assert.Single(events);
        Assert.IsAssignableFrom<IReadOnlyList<DomainEvent>>(events);
    }
}
