using FluentAssertions;
using MediatR;
using NSubstitute;
using VSA.Application;
using VSA.Application.Common;
using VSA.Application.Interfaces;
using VSA.Blazor.Services;

namespace VSA.Blazor.Tests;

public class MediatorServiceTests
{
    private readonly IMediator _mediator;
    private readonly MediatorService _sut;

    public MediatorServiceTests()
    {
        _mediator = Substitute.For<IMediator>();
        _sut = new MediatorService(_mediator);
    }

    #region QueryAsync Tests

    public record TestDto(string Name);
    public record TestQuery(string Term) : IQuery<Result<IReadOnlyList<TestDto>>>;

    [Fact]
    public async Task QueryAsync_Success_ShouldReturnValue()
    {
        // Arrange
        IReadOnlyList<TestDto> expectedItems = new List<TestDto> { new("Item1"), new("Item2") };
        var query = new TestQuery("test");

        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expectedItems));

        // Act
        var result = await _sut.QueryAsync(query);

        // Assert
        result.Should().BeEquivalentTo(expectedItems);
    }

    [Fact]
    public async Task QueryAsync_Failure_ShouldReturnDefaultAndCallOnError()
    {
        // Arrange
        var query = new TestQuery("test");
        var errorMessage = "クエリエラー";
        string? capturedError = null;

        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<IReadOnlyList<TestDto>>(errorMessage));

        // Act
        var result = await _sut.QueryAsync(query, error => capturedError = error);

        // Assert
        result.Should().BeNull();
        capturedError.Should().Be(errorMessage);
    }

    [Fact]
    public async Task QueryAsync_Failure_NoCallback_ShouldNotThrow()
    {
        // Arrange
        var query = new TestQuery("test");

        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<IReadOnlyList<TestDto>>("エラー"));

        // Act
        var act = () => _sut.QueryAsync(query);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region QueryPagedAsync Tests

    public record TestPagedQuery(int Page) : IQuery<Result<PagedResult<TestDto>>>;

    [Fact]
    public async Task QueryPagedAsync_Success_ShouldReturnPagedResult()
    {
        // Arrange
        var items = new List<TestDto> { new("Item1") };
        var pagedResult = PagedResult<TestDto>.Create(items, 10, 1, 5);
        var query = new TestPagedQuery(1);

        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(Result.Success(pagedResult));

        // Act
        var result = await _sut.QueryPagedAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(10);
        result.CurrentPage.Should().Be(1);
    }

    [Fact]
    public async Task QueryPagedAsync_Failure_ShouldReturnNullAndCallOnError()
    {
        // Arrange
        var query = new TestPagedQuery(1);
        string? capturedError = null;

        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<PagedResult<TestDto>>("ページングエラー"));

        // Act
        var result = await _sut.QueryPagedAsync(query, error => capturedError = error);

        // Assert
        result.Should().BeNull();
        capturedError.Should().Be("ページングエラー");
    }

    #endregion

    #region CommandAsync<TResult> Tests

    public record CreateCommand(string Name) : ICommand<Result<Guid>>;

    [Fact]
    public async Task CommandAsync_WithResult_Success_ShouldReturnValue()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var command = new CreateCommand("Test");

        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expectedId));

        // Act
        var result = await _sut.CommandAsync(command);

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task CommandAsync_WithResult_Failure_ShouldReturnDefaultAndCallOnError()
    {
        // Arrange
        var command = new CreateCommand("Test");
        string? capturedError = null;

        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<Guid>("作成エラー"));

        // Act
        var result = await _sut.CommandAsync(command, error => capturedError = error);

        // Assert
        result.Should().Be(Guid.Empty);
        capturedError.Should().Be("作成エラー");
    }

    #endregion

    #region CommandAsync (no result) Tests

    public record DeleteCommand(Guid Id) : ICommand<Result>;

    [Fact]
    public async Task CommandAsync_NoResult_Success_ShouldReturnTrue()
    {
        // Arrange
        var command = new DeleteCommand(Guid.NewGuid());

        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _sut.CommandAsync(command);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CommandAsync_NoResult_Failure_ShouldReturnFalseAndCallOnError()
    {
        // Arrange
        var command = new DeleteCommand(Guid.NewGuid());
        string? capturedError = null;

        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("削除エラー"));

        // Act
        var result = await _sut.CommandAsync(command, error => capturedError = error);

        // Assert
        result.Should().BeFalse();
        capturedError.Should().Be("削除エラー");
    }

    #endregion

    #region Default Error Message Tests

    [Fact]
    public async Task QueryAsync_Failure_NullError_ShouldUseDefaultMessage()
    {
        // Arrange
        var query = new TestQuery("test");
        string? capturedError = null;

        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<IReadOnlyList<TestDto>>(null!));

        // Act
        await _sut.QueryAsync(query, error => capturedError = error);

        // Assert
        capturedError.Should().Be("エラーが発生しました");
    }

    #endregion
}
