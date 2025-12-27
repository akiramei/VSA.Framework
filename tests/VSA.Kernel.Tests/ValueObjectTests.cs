using VSA.Kernel;

namespace VSA.Kernel.Tests;

public class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }
        public string? ZipCode { get; }

        public Address(string street, string city, string? zipCode = null)
        {
            Street = street;
            City = city;
            ZipCode = zipCode;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return ZipCode;
        }
    }

    [Fact]
    public void Equals_SameValues_ShouldReturnTrue()
    {
        // Arrange
        var money1 = new Money(100, "JPY");
        var money2 = new Money(100, "JPY");

        // Act & Assert
        Assert.Equal(money1, money2);
    }

    [Fact]
    public void Equals_DifferentAmount_ShouldReturnFalse()
    {
        // Arrange
        var money1 = new Money(100, "JPY");
        var money2 = new Money(200, "JPY");

        // Act & Assert
        Assert.NotEqual(money1, money2);
    }

    [Fact]
    public void Equals_DifferentCurrency_ShouldReturnFalse()
    {
        // Arrange
        var money1 = new Money(100, "JPY");
        var money2 = new Money(100, "USD");

        // Act & Assert
        Assert.NotEqual(money1, money2);
    }

    [Fact]
    public void EqualityOperator_SameValues_ShouldReturnTrue()
    {
        // Arrange
        var money1 = new Money(100, "JPY");
        var money2 = new Money(100, "JPY");

        // Act & Assert
        Assert.True(money1 == money2);
    }

    [Fact]
    public void InequalityOperator_DifferentValues_ShouldReturnTrue()
    {
        // Arrange
        var money1 = new Money(100, "JPY");
        var money2 = new Money(200, "JPY");

        // Act & Assert
        Assert.True(money1 != money2);
    }

    [Fact]
    public void GetHashCode_SameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var money1 = new Money(100, "JPY");
        var money2 = new Money(100, "JPY");

        // Act & Assert
        Assert.Equal(money1.GetHashCode(), money2.GetHashCode());
    }

    [Fact]
    public void Equals_WithNullComponent_ShouldWork()
    {
        // Arrange
        var address1 = new Address("Main St", "Tokyo", null);
        var address2 = new Address("Main St", "Tokyo", null);

        // Act & Assert
        Assert.Equal(address1, address2);
    }

    [Fact]
    public void Equals_WithNullVsNonNull_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("Main St", "Tokyo", null);
        var address2 = new Address("Main St", "Tokyo", "100-0001");

        // Act & Assert
        Assert.NotEqual(address1, address2);
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var money = new Money(100, "JPY");

        // Act & Assert
        Assert.False(money.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var money = new Money(100, "JPY");
        var address = new Address("Main St", "Tokyo");

        // Act & Assert
        Assert.False(money.Equals(address));
    }
}
