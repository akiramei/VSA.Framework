using FluentAssertions;
using VSA.Kernel;

namespace VSA.Kernel.Tests;

public class TypedIdTests
{
    // Test TypedId implementation
    private sealed class ProductId : TypedId<ProductId>
    {
        public ProductId(Guid value) : base(value) { }
        public static ProductId New() => new(Guid.NewGuid());
    }

    private sealed class OrderId : TypedId<OrderId>
    {
        public OrderId(Guid value) : base(value) { }
        public static OrderId New() => new(Guid.NewGuid());
    }

    [Fact]
    public void Constructor_WithValidGuid_ShouldCreateInstance()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var productId = new ProductId(guid);

        // Assert
        productId.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new ProductId(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ProductId*空*");
    }

    [Fact]
    public void New_ShouldCreateUniqueIds()
    {
        // Act
        var id1 = ProductId.New();
        var id2 = ProductId.New();

        // Assert
        id1.Value.Should().NotBe(Guid.Empty);
        id2.Value.Should().NotBe(Guid.Empty);
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new ProductId(guid);
        var id2 = new ProductId(guid);

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        (id1 != id2).Should().BeFalse();
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var id1 = ProductId.New();
        var id2 = ProductId.New();

        // Assert
        id1.Should().NotBe(id2);
        (id1 == id2).Should().BeFalse();
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentTypes_ShouldNotBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var productId = new ProductId(guid);
        var orderId = new OrderId(guid);

        // Assert - Different types with same Guid should not be equal
        productId.Equals(orderId).Should().BeFalse();
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ShouldWork()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var productId = new ProductId(guid);

        // Act
        Guid converted = productId;

        // Assert
        converted.Should().Be(guid);
    }

    [Fact]
    public void CompareTo_ShouldOrderCorrectly()
    {
        // Arrange
        var guid1 = new Guid("00000000-0000-0000-0000-000000000001");
        var guid2 = new Guid("00000000-0000-0000-0000-000000000002");
        var id1 = new ProductId(guid1);
        var id2 = new ProductId(guid2);

        // Assert
        id1.CompareTo(id2).Should().BeLessThan(0);
        id2.CompareTo(id1).Should().BeGreaterThan(0);
        id1.CompareTo(id1).Should().Be(0);
    }

    [Fact]
    public void CompareTo_WithNull_ShouldReturnPositive()
    {
        // Arrange
        var id = ProductId.New();

        // Act
        var result = id.CompareTo(null);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var productId = new ProductId(guid);

        // Act
        var str = productId.ToString();

        // Assert
        str.Should().Be(guid.ToString());
    }

    [Fact]
    public void TryParse_WithValidString_ShouldSucceed()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var str = guid.ToString();

        // Act
        var result = TypedId<ProductId>.TryParse(str, out var productId);

        // Assert
        result.Should().BeTrue();
        productId.Should().NotBeNull();
        productId!.Value.Should().Be(guid);
    }

    [Fact]
    public void TryParse_WithEmptyGuidString_ShouldFail()
    {
        // Act
        var result = TypedId<ProductId>.TryParse(Guid.Empty.ToString(), out var productId);

        // Assert
        result.Should().BeFalse();
        productId.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithInvalidString_ShouldFail()
    {
        // Act
        var result = TypedId<ProductId>.TryParse("invalid", out var productId);

        // Assert
        result.Should().BeFalse();
        productId.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithNullOrEmpty_ShouldFail()
    {
        // Assert
        TypedId<ProductId>.TryParse(null, out _).Should().BeFalse();
        TypedId<ProductId>.TryParse("", out _).Should().BeFalse();
        TypedId<ProductId>.TryParse("  ", out _).Should().BeFalse();
    }
}
