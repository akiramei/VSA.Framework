using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VSA.Application;
using VSA.Infrastructure.Abstractions;
using VSA.Infrastructure.Behaviors;

namespace VSA.Infrastructure.Tests.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldLogRequestStartAndComplete()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LoggingBehavior<TestQuery, Result<string>>>>();
        loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var correlationIdMock = new Mock<ICorrelationIdAccessor>();
        correlationIdMock.Setup(x => x.CorrelationId).Returns("corr-456");
        var behavior = new LoggingBehavior<TestQuery, Result<string>>(
            loggerMock.Object,
            correlationIdMock.Object);
        var query = new TestQuery("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => Task.FromResult(Result.Success("success"));

        // Act
        await behavior.Handle(query, next, CancellationToken.None);

        // Assert - verify logging was called
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("処理開始")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallNextAndReturnResult()
    {
        // Arrange
        var logger = NullLogger<LoggingBehavior<TestQuery, Result<string>>>.Instance;
        var correlationIdMock = new Mock<ICorrelationIdAccessor>();
        correlationIdMock.Setup(x => x.CorrelationId).Returns("corr-456");
        var behavior = new LoggingBehavior<TestQuery, Result<string>>(
            logger,
            correlationIdMock.Object);
        var query = new TestQuery("test");
        var callCount = 0;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            callCount++;
            return Task.FromResult(Result.Success("success"));
        };

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("success");
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithNullCorrelationIdAccessor_ShouldStillWork()
    {
        // Arrange
        var logger = NullLogger<LoggingBehavior<TestQuery, Result<string>>>.Instance;
        var behavior = new LoggingBehavior<TestQuery, Result<string>>(
            logger,
            null);
        var query = new TestQuery("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => Task.FromResult(Result.Success("success"));

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        var loggerMock = new Mock<ILogger<LoggingBehavior<TestQuery, Result<string>>>>();
        loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var correlationIdMock = new Mock<ICorrelationIdAccessor>();
        var behavior = new LoggingBehavior<TestQuery, Result<string>>(
            loggerMock.Object,
            correlationIdMock.Object);
        var query = new TestQuery("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => throw exception;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(query, next, CancellationToken.None));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("処理失敗")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
