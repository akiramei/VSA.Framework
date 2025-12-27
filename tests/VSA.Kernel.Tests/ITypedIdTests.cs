using FluentAssertions;
using VSA.Kernel;

namespace VSA.Kernel.Tests;

public class ITypedIdTests
{
    // Test implementation using readonly record struct
    private readonly record struct BookId(Guid Value) : ITypedId
    {
        public static BookId NewId() => new(Guid.NewGuid());
        public static BookId From(Guid value) => new(value);
    }

    private readonly record struct MemberId(Guid Value) : ITypedId
    {
        public static MemberId NewId() => new(Guid.NewGuid());
        public static MemberId From(Guid value) => new(value);
    }

    [Fact]
    public void NewId_ShouldCreateUniqueIds()
    {
        // Act
        var id1 = BookId.NewId();
        var id2 = BookId.NewId();

        // Assert
        id1.Value.Should().NotBe(Guid.Empty);
        id2.Value.Should().NotBe(Guid.Empty);
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void From_ShouldCreateIdFromGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = BookId.From(guid);

        // Assert
        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = BookId.From(guid);
        var id2 = BookId.From(guid);

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var id1 = BookId.NewId();
        var id2 = BookId.NewId();

        // Assert
        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentTypes_ShouldNotBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var bookId = BookId.From(guid);
        var memberId = MemberId.From(guid);

        // Assert - Different types with same Guid should not be equal
        bookId.Equals(memberId).Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_WithEmptyGuid_ShouldReturnTrue()
    {
        // Arrange
        var id = new BookId(Guid.Empty);

        // Assert
        id.IsEmpty().Should().BeTrue();
        id.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_WithValidGuid_ShouldReturnFalse()
    {
        // Arrange
        var id = BookId.NewId();

        // Assert
        id.IsEmpty().Should().BeFalse();
        id.IsValid().Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = BookId.From(guid);

        // Act
        var str = id.ToString();

        // Assert
        str.Should().Contain(guid.ToString());
    }

    [Fact]
    public void StructSemantics_ShouldBehaveAsValueType()
    {
        // Arrange
        var original = BookId.NewId();
        var copy = original;

        // Assert - both should have the same value (value type semantics)
        copy.Value.Should().Be(original.Value);
    }
}
