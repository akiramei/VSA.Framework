using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Handlers.Abstractions;
using VSA.Handlers.Commands;

namespace VSA.Handlers.Tests.Commands;

public class UpdateEntityHandlerTests
{
    private readonly InMemoryRepository<TestProduct, Guid> _repository;

    public UpdateEntityHandlerTests()
    {
        _repository = new InMemoryRepository<TestProduct, Guid>();
    }

    [Fact]
    public async Task Handle_ExistingEntity_ShouldUpdateSuccessfully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new TestProduct(productId, "Original Name", 100m);
        _repository.Seed(product);

        var updater = new TestProductUpdater();
        var logger = NullLogger<UpdateEntityWithUpdaterHandler<UpdateProductCommand, TestProduct, Guid>>.Instance;
        var handler = new UpdateEntityWithUpdaterHandler<UpdateProductCommand, TestProduct, Guid>(
            _repository, updater, logger);
        var command = new UpdateProductCommand(productId, "Updated Name", 200m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = await _repository.GetByIdAsync(productId);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Price.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ShouldReturnFailure()
    {
        // Arrange
        var updater = new TestProductUpdater();
        var logger = NullLogger<UpdateEntityWithUpdaterHandler<UpdateProductCommand, TestProduct, Guid>>.Instance;
        var handler = new UpdateEntityWithUpdaterHandler<UpdateProductCommand, TestProduct, Guid>(
            _repository, updater, logger);
        var command = new UpdateProductCommand(Guid.NewGuid(), "Updated Name", 200m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("見つかりません");
    }

    [Fact]
    public async Task Handle_WithVersionMismatch_ShouldReturnFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new TestProduct(productId, "Original Name", 100m);
        _repository.Seed(product);

        var logger = NullLogger<TestVersionedUpdateHandler>.Instance;
        var handler = new TestVersionedUpdateHandler(_repository, logger);
        var command = new VersionedUpdateProductCommand(productId, "Updated Name", 200m, 999); // Wrong version

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("更新されています");
    }

    // Test Updater
    private class TestProductUpdater : IEntityUpdater<UpdateProductCommand, TestProduct>
    {
        public void Update(TestProduct entity, UpdateProductCommand command)
        {
            entity.UpdateName(command.Name);
            entity.UpdatePrice(command.Price);
        }
    }

    // Versioned command for testing optimistic concurrency
    private record VersionedUpdateProductCommand(Guid Id, string Name, decimal Price, long ExpectedVersion)
        : ICommand<Result>, IVersionedCommand<Guid>;

    // Handler for versioned commands
    private class TestVersionedUpdateHandler : UpdateEntityHandler<VersionedUpdateProductCommand, TestProduct, Guid>
    {
        public TestVersionedUpdateHandler(
            IRepository<TestProduct, Guid> repository,
            Microsoft.Extensions.Logging.ILogger logger)
            : base(repository, logger)
        {
        }

        protected override void UpdateEntity(TestProduct entity, VersionedUpdateProductCommand command)
        {
            entity.UpdateName(command.Name);
            entity.UpdatePrice(command.Price);
        }
    }
}
