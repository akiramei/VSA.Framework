using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VSA.Application;
using VSA.Infrastructure.Behaviors;

namespace VSA.Infrastructure.Tests.Behaviors;

public class PerformanceBehaviorTests
{
    [Fact]
    public async Task Handle_FastRequest_ShouldNotLogWarning()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceBehavior<TestQuery, Result<string>>>>();
        loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var behavior = new PerformanceBehavior<TestQuery, Result<string>>(loggerMock.Object, 500);
        var query = new TestQuery("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => Task.FromResult(Result.Success("success"));

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SlowRequest_ShouldLogWarning()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceBehavior<TestQuery, Result<string>>>>();
        loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var behavior = new PerformanceBehavior<TestQuery, Result<string>>(loggerMock.Object, 100);
        var query = new TestQuery("test");
        RequestHandlerDelegate<Result<string>> next = async (ct) =>
        {
            await Task.Delay(150); // Delay longer than threshold
            return Result.Success("success");
        };

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("長時間リクエスト")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnResultFromNext()
    {
        // Arrange
        var logger = NullLogger<PerformanceBehavior<TestQuery, Result<string>>>.Instance;
        var behavior = new PerformanceBehavior<TestQuery, Result<string>>(logger);
        var query = new TestQuery("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => Task.FromResult(Result.Success("expected-value"));

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        result.Value.Should().Be("expected-value");
    }
}
