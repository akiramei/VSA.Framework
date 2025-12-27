using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VSA.Application;
using VSA.Infrastructure.Abstractions;
using VSA.Infrastructure.Behaviors;

namespace VSA.Infrastructure.Tests.Behaviors;

public class CachingBehaviorTests
{
    private readonly IMemoryCache _cache;
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public CachingBehaviorTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserMock.Setup(x => x.TenantId).Returns((Guid?)null);
    }

    [Fact]
    public async Task Handle_CacheableQuery_ShouldCacheResult()
    {
        // Arrange
        var logger = NullLogger<CachingBehavior<TestCacheableQuery, Result<string>>>.Instance;
        var behavior = new CachingBehavior<TestCacheableQuery, Result<string>>(
            _cache, logger, _currentUserMock.Object);
        var query = new TestCacheableQuery("test");
        var callCount = 0;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            callCount++;
            return Task.FromResult(Result.Success("cached-value"));
        };

        // Act - First call
        var result1 = await behavior.Handle(query, next, CancellationToken.None);

        // Act - Second call (should be from cache)
        var result2 = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        result1.Value.Should().Be("cached-value");
        result2.Value.Should().Be("cached-value");
        callCount.Should().Be(1); // Only called once due to caching
    }

    [Fact]
    public async Task Handle_DifferentQueries_ShouldCacheSeparately()
    {
        // Arrange
        var logger = NullLogger<CachingBehavior<TestCacheableQuery, Result<string>>>.Instance;
        var behavior = new CachingBehavior<TestCacheableQuery, Result<string>>(
            _cache, logger, _currentUserMock.Object);
        var query1 = new TestCacheableQuery("test1");
        var query2 = new TestCacheableQuery("test2");
        var callCount = 0;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            callCount++;
            return Task.FromResult(Result.Success($"value-{callCount}"));
        };

        // Act
        var result1 = await behavior.Handle(query1, next, CancellationToken.None);
        var result2 = await behavior.Handle(query2, next, CancellationToken.None);

        // Assert
        result1.Value.Should().Be("value-1");
        result2.Value.Should().Be("value-2");
        callCount.Should().Be(2); // Called twice for different queries
    }

    [Fact]
    public async Task Handle_WithoutCurrentUserService_ShouldUseAnonymous()
    {
        // Arrange
        var logger = NullLogger<CachingBehavior<TestCacheableQuery, Result<string>>>.Instance;
        var behavior = new CachingBehavior<TestCacheableQuery, Result<string>>(
            _cache, logger, null);
        var query = new TestCacheableQuery("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => Task.FromResult(Result.Success("value"));

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        result.Value.Should().Be("value");
    }
}
