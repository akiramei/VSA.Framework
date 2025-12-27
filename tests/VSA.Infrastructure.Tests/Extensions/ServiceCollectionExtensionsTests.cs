using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using VSA.Infrastructure.Behaviors;
using VSA.Infrastructure.Extensions;

namespace VSA.Infrastructure.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddVsaFramework_ShouldRegisterMediatR()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddVsaFramework(typeof(ServiceCollectionExtensionsTests).Assembly);

        // Assert - verify service is registered without resolving
        var mediatorDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));
        mediatorDescriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddVsaFramework_ShouldRegisterMemoryCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddVsaFramework(typeof(ServiceCollectionExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var cache = provider.GetService<IMemoryCache>();
        cache.Should().NotBeNull();
    }

    [Fact]
    public void AddVsaBehaviors_ShouldRegisterAllBehaviorsByDefault()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVsaBehaviors();

        // Assert
        var behaviorDescriptors = services
            .Where(s => s.ServiceType.IsGenericType &&
                       s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();

        behaviorDescriptors.Should().HaveCount(9); // All 9 behaviors
    }

    [Fact]
    public void AddVsaBehaviors_WithDisabledValidation_ShouldNotRegisterValidationBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVsaBehaviors(options => options.EnableValidation = false);

        // Assert
        var behaviorDescriptors = services
            .Where(s => s.ServiceType.IsGenericType &&
                       s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();

        behaviorDescriptors.Should().HaveCount(8); // All except ValidationBehavior
    }

    [Fact]
    public void AddVsaBehaviors_WithDisabledLogging_ShouldNotRegisterLoggingBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVsaBehaviors(options => options.EnableLogging = false);

        // Assert
        var behaviorDescriptors = services
            .Where(s => s.ServiceType.IsGenericType &&
                       s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();

        behaviorDescriptors.Should().HaveCount(8); // All except LoggingBehavior
    }

    [Fact]
    public void AddVsaBehaviors_WithAllDisabled_ShouldRegisterNoBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVsaBehaviors(options =>
        {
            options.EnableExceptionHandling = false;
            options.EnablePerformanceMonitoring = false;
            options.EnableValidation = false;
            options.EnableAuthorization = false;
            options.EnableIdempotency = false;
            options.EnableCaching = false;
            options.EnableTransaction = false;
            options.EnableAuditLog = false;
            options.EnableLogging = false;
        });

        // Assert
        var behaviorDescriptors = services
            .Where(s => s.ServiceType.IsGenericType &&
                       s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();

        behaviorDescriptors.Should().HaveCount(0);
    }

    [Fact]
    public void VsaBehaviorOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new VsaBehaviorOptions();

        // Assert
        options.EnableExceptionHandling.Should().BeTrue();
        options.EnablePerformanceMonitoring.Should().BeTrue();
        options.EnableValidation.Should().BeTrue();
        options.EnableAuthorization.Should().BeTrue();
        options.EnableIdempotency.Should().BeTrue();
        options.EnableCaching.Should().BeTrue();
        options.EnableTransaction.Should().BeTrue();
        options.EnableAuditLog.Should().BeTrue();
        options.EnableLogging.Should().BeTrue();
        options.SlowRequestThresholdMs.Should().Be(500);
    }

    [Fact]
    public void AddVsaMediatR_ShouldOnlyRegisterMediatR()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVsaMediatR(typeof(ServiceCollectionExtensionsTests).Assembly);

        // Assert - verify service is registered without resolving
        var mediatorDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));
        mediatorDescriptor.Should().NotBeNull();

        // No behaviors should be registered
        var behaviorDescriptors = services
            .Where(s => s.ServiceType.IsGenericType &&
                       s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();
        behaviorDescriptors.Should().HaveCount(0);
    }
}
