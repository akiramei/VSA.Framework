using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using VSA.Handlers.Abstractions;
using VSA.Handlers.Extensions;

namespace VSA.Handlers.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddVsaHandlers_ShouldRegisterEntityFactories()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVsaHandlers(typeof(ServiceCollectionExtensionsTests).Assembly);

        // Assert - check that factory interface is registered
        var factoryDescriptor = services.FirstOrDefault(s =>
            s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(IEntityFactory<,>));

        factoryDescriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddVsaHandlers_ShouldRegisterEntityUpdaters()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVsaHandlers(typeof(ServiceCollectionExtensionsTests).Assembly);

        // Assert - check that updater interface is registered
        var updaterDescriptor = services.FirstOrDefault(s =>
            s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(IEntityUpdater<,>));

        updaterDescriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddVsaHandlers_WithNoMatchingTypes_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddVsaHandlers(typeof(object).Assembly);

        // Assert
        act.Should().NotThrow();
    }
}

// Test implementations for scanning
public class TestProductFactory : IEntityFactory<CreateProductCommand, TestProduct>
{
    public TestProduct Create(CreateProductCommand command)
    {
        return new TestProduct(Guid.NewGuid(), command.Name, command.Price);
    }
}

public class TestProductUpdater : IEntityUpdater<UpdateProductCommand, TestProduct>
{
    public void Update(TestProduct entity, UpdateProductCommand command)
    {
        entity.UpdateName(command.Name);
        entity.UpdatePrice(command.Price);
    }
}
