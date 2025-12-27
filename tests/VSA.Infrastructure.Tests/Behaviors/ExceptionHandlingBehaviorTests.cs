using FluentAssertions;
using MediatR;
using VSA.Application;
using VSA.Infrastructure.Behaviors;
using VSA.Kernel;

namespace VSA.Infrastructure.Tests.Behaviors;

public class ExceptionHandlingBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoException_ShouldReturnSuccess()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<TestCommand, Result<string>>();
        var command = new TestCommand("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => Task.FromResult(Result.Success("success"));

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("success");
    }

    [Fact]
    public async Task Handle_WithDomainException_ShouldReturnFailureWithMessage()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<TestCommand, Result<string>>();
        var command = new TestCommand("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => throw new DomainException("Domain error occurred");

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Domain error occurred");
    }

    [Fact]
    public async Task Handle_WithGenericException_ShouldReturnGenericError()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<TestCommand, Result<string>>();
        var command = new TestCommand("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => throw new InvalidOperationException("Something went wrong");

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("予期しないエラーが発生しました");
    }

    [Fact]
    public async Task Handle_WithOperationCanceledException_ShouldRethrow()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<TestCommand, Result<string>>();
        var command = new TestCommand("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => throw new OperationCanceledException();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => behavior.Handle(command, next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonResultType_ShouldRethrowException()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<TestNonResultCommand, string>();
        var command = new TestNonResultCommand("test");
        RequestHandlerDelegate<string> next = (ct) => throw new DomainException("Domain error");

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => behavior.Handle(command, next, CancellationToken.None));
    }
}
