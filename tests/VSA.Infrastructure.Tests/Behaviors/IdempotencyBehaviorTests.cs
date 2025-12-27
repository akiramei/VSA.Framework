using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Infrastructure.Abstractions;
using VSA.Infrastructure.Behaviors;

namespace VSA.Infrastructure.Tests.Behaviors;

public class IdempotencyBehaviorTests
{
    private readonly InMemoryIdempotencyStore _store;

    public IdempotencyBehaviorTests()
    {
        _store = new InMemoryIdempotencyStore();
    }

    [Fact]
    public async Task Handle_WithIdempotentCommand_FirstRequest_ShouldExecuteAndSave()
    {
        // Arrange
        var logger = NullLogger<IdempotencyBehavior<TestIdempotentCommand, Result<Guid>>>.Instance;
        var behavior = new IdempotencyBehavior<TestIdempotentCommand, Result<Guid>>(logger, _store);

        var expectedId = Guid.NewGuid();
        var command = new TestIdempotentCommand("key-1", "Test");
        var executionCount = 0;

        // Act
        var result = await behavior.Handle(
            command,
            (ct) =>
            {
                executionCount++;
                return Task.FromResult(Result.Success(expectedId));
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedId);
        executionCount.Should().Be(1);
        _store.Records.Should().ContainKey("key-1");
        _store.Records["key-1"].Status.Should().Be(IdempotencyStatus.Completed);
    }

    [Fact]
    public async Task Handle_WithIdempotentCommand_DuplicateRequest_ShouldReturnCachedResult()
    {
        // Arrange
        // Note: This test uses a string response because Result<T> requires special JSON handling
        var logger = NullLogger<IdempotencyBehavior<TestIdempotentStringCommand, string>>.Instance;
        var behavior = new IdempotencyBehavior<TestIdempotentStringCommand, string>(logger, _store);

        var expectedValue = "test-result-123";
        var command = new TestIdempotentStringCommand("key-2", "Test");
        var executionCount = 0;

        // First request
        await behavior.Handle(
            command,
            (ct) =>
            {
                executionCount++;
                return Task.FromResult(expectedValue);
            },
            CancellationToken.None);

        // Act - Second request with same key
        var result = await behavior.Handle(
            command,
            (ct) =>
            {
                executionCount++;
                return Task.FromResult("different-result"); // Different value
            },
            CancellationToken.None);

        // Assert
        result.Should().Be(expectedValue); // Should return cached result
        executionCount.Should().Be(1); // Should only execute once
    }

    [Fact]
    public async Task Handle_WithNonIdempotentCommand_ShouldExecuteNormally()
    {
        // Arrange
        var logger = NullLogger<IdempotencyBehavior<TestNonIdempotentCommand, Result>>.Instance;
        var behavior = new IdempotencyBehavior<TestNonIdempotentCommand, Result>(logger, _store);

        var command = new TestNonIdempotentCommand("Test");
        var executionCount = 0;

        // Act
        var result = await behavior.Handle(
            command,
            (ct) =>
            {
                executionCount++;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        executionCount.Should().Be(1);
        _store.Records.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithException_ShouldRemoveRecordAndRethrow()
    {
        // Arrange
        var logger = NullLogger<IdempotencyBehavior<TestIdempotentCommand, Result<Guid>>>.Instance;
        var behavior = new IdempotencyBehavior<TestIdempotentCommand, Result<Guid>>(logger, _store);

        var command = new TestIdempotentCommand("key-3", "Test");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await behavior.Handle(
                command,
                (ct) => throw new InvalidOperationException("Test exception"),
                CancellationToken.None);
        });

        // Record should be removed for retry
        _store.Records.Should().NotContainKey("key-3");
    }

    [Fact]
    public async Task Handle_WithEmptyKey_ShouldSkipIdempotencyCheck()
    {
        // Arrange
        var logger = NullLogger<IdempotencyBehavior<TestIdempotentCommand, Result<Guid>>>.Instance;
        var behavior = new IdempotencyBehavior<TestIdempotentCommand, Result<Guid>>(logger, _store);

        var command = new TestIdempotentCommand("", "Test");
        var executionCount = 0;

        // Act
        var result = await behavior.Handle(
            command,
            (ct) =>
            {
                executionCount++;
                return Task.FromResult(Result.Success(Guid.NewGuid()));
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        executionCount.Should().Be(1);
        _store.Records.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithoutStore_ShouldSkipIdempotencyCheck()
    {
        // Arrange
        var logger = NullLogger<IdempotencyBehavior<TestIdempotentCommand, Result<Guid>>>.Instance;
        var behavior = new IdempotencyBehavior<TestIdempotentCommand, Result<Guid>>(
            logger,
            idempotencyStore: null);

        var command = new TestIdempotentCommand("key-4", "Test");
        var executionCount = 0;

        // Act - First request
        await behavior.Handle(
            command,
            (ct) =>
            {
                executionCount++;
                return Task.FromResult(Result.Success(Guid.NewGuid()));
            },
            CancellationToken.None);

        // Act - Second request (would normally be cached)
        await behavior.Handle(
            command,
            (ct) =>
            {
                executionCount++;
                return Task.FromResult(Result.Success(Guid.NewGuid()));
            },
            CancellationToken.None);

        // Assert
        executionCount.Should().Be(2); // Both should execute
    }

    [Fact]
    public async Task Handle_WithProcessingRecord_ShouldReturnConflict()
    {
        // Arrange
        var logger = NullLogger<IdempotencyBehavior<TestIdempotentCommand, Result<Guid>>>.Instance;
        var behavior = new IdempotencyBehavior<TestIdempotentCommand, Result<Guid>>(logger, _store);

        var command = new TestIdempotentCommand("key-5", "Test");

        // Pre-populate with processing record
        _store.Records["key-5"] = new IdempotencyRecord
        {
            Key = "key-5",
            RequestType = typeof(TestIdempotentCommand).FullName!,
            ResponseJson = "",
            ResponseType = typeof(Result<Guid>).FullName!,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = IdempotencyStatus.Processing
        };

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Success(Guid.NewGuid())),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("処理中");
    }

    // Test Commands
    private record TestIdempotentCommand(string IdempotencyKey, string Name) : ICommand<Result<Guid>>, IIdempotentCommand;

    private record TestIdempotentStringCommand(string IdempotencyKey, string Name) : ICommand<string>, IIdempotentCommand;

    private record TestNonIdempotentCommand(string Name) : ICommand<Result>;

    // Test Store
    private class InMemoryIdempotencyStore : IIdempotencyStore
    {
        public Dictionary<string, IdempotencyRecord> Records { get; } = new();

        public Task<IdempotencyRecord?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            Records.TryGetValue(key, out var record);
            return Task.FromResult(record);
        }

        public Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
        {
            Records[record.Key] = record;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Records.Remove(key);
            return Task.CompletedTask;
        }
    }
}
