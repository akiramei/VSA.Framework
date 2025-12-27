using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VSA.Application;
using VSA.Infrastructure.Abstractions;
using VSA.Infrastructure.Behaviors;

namespace VSA.Infrastructure.Tests.Behaviors;

public class TransactionBehaviorTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public TransactionBehaviorTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    [Fact]
    public async Task Handle_SuccessfulCommand_ShouldBeginSaveAndCommitTransaction()
    {
        // Arrange
        var logger = NullLogger<TransactionBehavior<TestCommand, Result<string>>>.Instance;
        var behavior = new TransactionBehavior<TestCommand, Result<string>>(
            _unitOfWorkMock.Object,
            logger);
        var command = new TestCommand("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => Task.FromResult(Result.Success("success"));

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FailedResult_ShouldRollback()
    {
        // Arrange
        var logger = NullLogger<TransactionBehavior<TestCommand, Result<string>>>.Instance;
        var behavior = new TransactionBehavior<TestCommand, Result<string>>(
            _unitOfWorkMock.Object,
            logger);
        var command = new TestCommand("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => Task.FromResult(Result.Fail<string>("Error"));

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Exception_ShouldRollbackAndRethrow()
    {
        // Arrange
        var logger = NullLogger<TransactionBehavior<TestCommand, Result<string>>>.Instance;
        var behavior = new TransactionBehavior<TestCommand, Result<string>>(
            _unitOfWorkMock.Object,
            logger);
        var command = new TestCommand("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => throw new InvalidOperationException("Error");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(command, next, CancellationToken.None));

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
