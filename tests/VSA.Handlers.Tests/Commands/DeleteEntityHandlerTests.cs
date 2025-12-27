using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VSA.Handlers.Commands;

namespace VSA.Handlers.Tests.Commands;

public class DeleteEntityHandlerTests
{
    private readonly InMemoryRepository<TestProduct, Guid> _repository;

    public DeleteEntityHandlerTests()
    {
        _repository = new InMemoryRepository<TestProduct, Guid>();
    }

    [Fact]
    public async Task Handle_ExistingEntity_ShouldSoftDelete()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new TestProduct(productId, "Test Product", 100m);
        _repository.Seed(product);

        var logger = NullLogger<SimpleDeleteEntityHandler<DeleteProductCommand, TestProduct, Guid>>.Instance;
        var handler = new SimpleDeleteEntityHandler<DeleteProductCommand, TestProduct, Guid>(
            _repository, logger);
        var command = new DeleteProductCommand(productId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var deleted = await _repository.GetByIdAsync(productId);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ShouldReturnFailure()
    {
        // Arrange
        var logger = NullLogger<SimpleDeleteEntityHandler<DeleteProductCommand, TestProduct, Guid>>.Instance;
        var handler = new SimpleDeleteEntityHandler<DeleteProductCommand, TestProduct, Guid>(
            _repository, logger);
        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("見つかりません");
    }

    [Fact]
    public async Task Handle_MultipleDeletes_ShouldSoftDeleteAll()
    {
        // Arrange
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var product1 = new TestProduct(product1Id, "Product 1", 100m);
        var product2 = new TestProduct(product2Id, "Product 2", 200m);
        _repository.Seed(product1, product2);

        var logger = NullLogger<SimpleDeleteEntityHandler<DeleteProductCommand, TestProduct, Guid>>.Instance;
        var handler = new SimpleDeleteEntityHandler<DeleteProductCommand, TestProduct, Guid>(
            _repository, logger);

        // Act
        await handler.Handle(new DeleteProductCommand(product1Id), CancellationToken.None);
        await handler.Handle(new DeleteProductCommand(product2Id), CancellationToken.None);

        // Assert
        var deleted1 = await _repository.GetByIdAsync(product1Id);
        var deleted2 = await _repository.GetByIdAsync(product2Id);

        deleted1!.IsDeleted.Should().BeTrue();
        deleted2!.IsDeleted.Should().BeTrue();
    }
}
