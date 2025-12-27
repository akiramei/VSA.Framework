using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Infrastructure.Abstractions;
using VSA.Infrastructure.Behaviors;

namespace VSA.Infrastructure.Tests.Behaviors;

public class AuditLogBehaviorTests
{
    private readonly InMemoryAuditLogRepository _auditLogRepository;
    private readonly TestCurrentUserService _currentUserService;

    public AuditLogBehaviorTests()
    {
        _auditLogRepository = new InMemoryAuditLogRepository();
        _currentUserService = new TestCurrentUserService(
            Guid.NewGuid(),
            "testuser",
            isAuthenticated: true);
    }

    [Fact]
    public async Task Handle_WithAuditableCommand_ShouldSaveAuditLog()
    {
        // Arrange
        var logger = NullLogger<AuditLogBehavior<TestAuditableCommand, Result>>.Instance;
        var behavior = new AuditLogBehavior<TestAuditableCommand, Result>(
            logger,
            _auditLogRepository,
            _currentUserService);

        var command = new TestAuditableCommand(Guid.NewGuid(), "Test");

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogRepository.Entries.Should().HaveCount(1);

        var entry = _auditLogRepository.Entries[0];
        entry.Action.Should().Be("TestAction");
        entry.EntityType.Should().Be("TestEntity");
        entry.EntityId.Should().Be(command.Id.ToString());
        entry.UserId.Should().Be(_currentUserService.UserId);
        entry.UserName.Should().Be(_currentUserService.UserName);
        entry.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithFailedResult_ShouldSaveAuditLogWithError()
    {
        // Arrange
        var logger = NullLogger<AuditLogBehavior<TestAuditableCommand, Result>>.Instance;
        var behavior = new AuditLogBehavior<TestAuditableCommand, Result>(
            logger,
            _auditLogRepository,
            _currentUserService);

        var command = new TestAuditableCommand(Guid.NewGuid(), "Test");

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Fail("Error occurred")),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _auditLogRepository.Entries.Should().HaveCount(1);

        var entry = _auditLogRepository.Entries[0];
        entry.IsSuccess.Should().BeFalse();
        entry.ErrorMessage.Should().Be("Error occurred");
    }

    [Fact]
    public async Task Handle_WithException_ShouldSaveAuditLogAndRethrow()
    {
        // Arrange
        var logger = NullLogger<AuditLogBehavior<TestAuditableCommand, Result>>.Instance;
        var behavior = new AuditLogBehavior<TestAuditableCommand, Result>(
            logger,
            _auditLogRepository,
            _currentUserService);

        var command = new TestAuditableCommand(Guid.NewGuid(), "Test");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await behavior.Handle(
                command,
                (ct) => throw new InvalidOperationException("Test exception"),
                CancellationToken.None);
        });

        _auditLogRepository.Entries.Should().HaveCount(1);
        var entry = _auditLogRepository.Entries[0];
        entry.IsSuccess.Should().BeFalse();
        entry.ErrorMessage.Should().Be("Test exception");
    }

    [Fact]
    public async Task Handle_WithNonAuditableCommand_ShouldSkipAuditLog()
    {
        // Arrange
        var logger = NullLogger<AuditLogBehavior<TestNonAuditableCommand, Result>>.Instance;
        var behavior = new AuditLogBehavior<TestNonAuditableCommand, Result>(
            logger,
            _auditLogRepository,
            _currentUserService);

        var command = new TestNonAuditableCommand("Test");

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogRepository.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithoutRepository_ShouldSkipAuditLog()
    {
        // Arrange
        var logger = NullLogger<AuditLogBehavior<TestAuditableCommand, Result>>.Instance;
        var behavior = new AuditLogBehavior<TestAuditableCommand, Result>(
            logger,
            auditLogRepository: null,
            _currentUserService);

        var command = new TestAuditableCommand(Guid.NewGuid(), "Test");

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // Test Commands
    private record TestAuditableCommand(Guid Id, string Name) : ICommand<Result>, IAuditableCommand
    {
        public AuditInfo GetAuditInfo() => new("TestAction", "TestEntity", Id.ToString());
    }

    private record TestNonAuditableCommand(string Name) : ICommand<Result>;

    // Test Repository
    private class InMemoryAuditLogRepository : IAuditLogRepository
    {
        public List<AuditLogEntry> Entries { get; } = new();

        public Task SaveAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }
}
