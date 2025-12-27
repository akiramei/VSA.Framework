using VSA.Kernel;

namespace VSA.Kernel.Tests;

public class AggregateRootTests
{
    private sealed class TestId : ValueObject
    {
        public Guid Value { get; }

        public TestId(Guid value)
        {
            Value = value;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public static TestId New() => new(Guid.NewGuid());
    }

    private sealed class TestAggregate : AggregateRoot<TestId>
    {
        public string Name { get; private set; } = string.Empty;

        private TestAggregate() { }

        public TestAggregate(TestId id, string name) : base(id)
        {
            Name = name;
        }
    }

    [Fact]
    public void Constructor_WithId_ShouldSetId()
    {
        // Arrange
        var id = TestId.New();

        // Act
        var aggregate = new TestAggregate(id, "Test");

        // Assert
        Assert.Equal(id, aggregate.Id);
    }

    [Fact]
    public void Constructor_WithNullId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestAggregate(null!, "Test"));
    }

    [Fact]
    public void Equals_SameId_ShouldReturnTrue()
    {
        // Arrange
        var id = TestId.New();
        var aggregate1 = new TestAggregate(id, "Test1");
        var aggregate2 = new TestAggregate(id, "Test2");

        // Act & Assert
        Assert.Equal(aggregate1, aggregate2);
    }

    [Fact]
    public void Equals_DifferentId_ShouldReturnFalse()
    {
        // Arrange
        var aggregate1 = new TestAggregate(TestId.New(), "Test");
        var aggregate2 = new TestAggregate(TestId.New(), "Test");

        // Act & Assert
        Assert.NotEqual(aggregate1, aggregate2);
    }

    [Fact]
    public void EqualityOperator_SameId_ShouldReturnTrue()
    {
        // Arrange
        var id = TestId.New();
        var aggregate1 = new TestAggregate(id, "Test1");
        var aggregate2 = new TestAggregate(id, "Test2");

        // Act & Assert
        Assert.True(aggregate1 == aggregate2);
    }

    [Fact]
    public void InequalityOperator_DifferentId_ShouldReturnTrue()
    {
        // Arrange
        var aggregate1 = new TestAggregate(TestId.New(), "Test");
        var aggregate2 = new TestAggregate(TestId.New(), "Test");

        // Act & Assert
        Assert.True(aggregate1 != aggregate2);
    }

    [Fact]
    public void GetHashCode_SameId_ShouldReturnSameHashCode()
    {
        // Arrange
        var id = TestId.New();
        var aggregate1 = new TestAggregate(id, "Test1");
        var aggregate2 = new TestAggregate(id, "Test2");

        // Act & Assert
        Assert.Equal(aggregate1.GetHashCode(), aggregate2.GetHashCode());
    }

    [Fact]
    public void Version_ShouldBeZeroByDefault()
    {
        // Arrange & Act
        var aggregate = new TestAggregate(TestId.New(), "Test");

        // Assert
        Assert.Equal(0, aggregate.Version);
    }
}
