using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VSA.Handlers.Queries;

namespace VSA.Handlers.Tests.Queries;

public class GetByIdQueryHandlerTests
{
    private readonly InMemoryRepository<TestProduct, Guid> _repository;

    public GetByIdQueryHandlerTests()
    {
        _repository = new InMemoryRepository<TestProduct, Guid>();
    }

    [Fact]
    public async Task Handle_ExistingEntity_ShouldReturnEntity()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new TestProduct(productId, "Test Product", 100m);
        _repository.Seed(product);

        var logger = NullLogger<SimpleGetByIdQueryHandler<GetProductByIdQuery, TestProduct, Guid>>.Instance;
        var handler = new SimpleGetByIdQueryHandler<GetProductByIdQuery, TestProduct, Guid>(
            _repository, logger);
        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(productId);
        result.Value.Name.Should().Be("Test Product");
        result.Value.Price.Should().Be(100m);
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ShouldReturnFailure()
    {
        // Arrange
        var logger = NullLogger<SimpleGetByIdQueryHandler<GetProductByIdQuery, TestProduct, Guid>>.Instance;
        var handler = new SimpleGetByIdQueryHandler<GetProductByIdQuery, TestProduct, Guid>(
            _repository, logger);
        var query = new GetProductByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("見つかりません");
    }

    [Fact]
    public async Task Handle_DeletedEntity_ShouldReturnFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new TestProduct(productId, "Deleted Product", 100m);
        product.Delete();
        _repository.Seed(product);

        var logger = NullLogger<SimpleGetByIdQueryHandler<GetProductByIdQuery, TestProduct, Guid>>.Instance;
        var handler = new SimpleGetByIdQueryHandler<GetProductByIdQuery, TestProduct, Guid>(
            _repository, logger);
        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("削除されています");
    }
}
