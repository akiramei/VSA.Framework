using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VSA.Application;
using VSA.Handlers.Abstractions;
using VSA.Handlers.Commands;

namespace VSA.Handlers.Tests.Commands;

public class CreateEntityHandlerTests
{
    private readonly InMemoryRepository<TestProduct, Guid> _repository;

    public CreateEntityHandlerTests()
    {
        _repository = new InMemoryRepository<TestProduct, Guid>();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateEntity()
    {
        // Arrange
        var factory = new TestProductFactory();
        var logger = NullLogger<CreateEntityWithFactoryHandler<CreateProductCommand, TestProduct, Guid>>.Instance;
        var handler = new CreateEntityWithFactoryHandler<CreateProductCommand, TestProduct, Guid>(
            _repository, factory, logger);
        var command = new CreateProductCommand("Test Product", 100m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        var created = await _repository.GetByIdAsync(result.Value);
        created.Should().NotBeNull();
        created!.Name.Should().Be("Test Product");
        created.Price.Should().Be(100m);
    }

    [Fact]
    public async Task Handle_MultipleCalls_ShouldCreateMultipleEntities()
    {
        // Arrange
        var factory = new TestProductFactory();
        var logger = NullLogger<CreateEntityWithFactoryHandler<CreateProductCommand, TestProduct, Guid>>.Instance;
        var handler = new CreateEntityWithFactoryHandler<CreateProductCommand, TestProduct, Guid>(
            _repository, factory, logger);

        // Act
        var result1 = await handler.Handle(new CreateProductCommand("Product 1", 100m), CancellationToken.None);
        var result2 = await handler.Handle(new CreateProductCommand("Product 2", 200m), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value);

        var count = await _repository.CountAsync();
        count.Should().Be(2);
    }

    // Test Factory
    private class TestProductFactory : IEntityFactory<CreateProductCommand, TestProduct>
    {
        public TestProduct Create(CreateProductCommand command)
        {
            return new TestProduct(Guid.NewGuid(), command.Name, command.Price);
        }
    }
}

// Custom handler test
public class CustomCreateEntityHandlerTests
{
    private readonly InMemoryRepository<TestProduct, Guid> _repository;

    public CustomCreateEntityHandlerTests()
    {
        _repository = new InMemoryRepository<TestProduct, Guid>();
    }

    [Fact]
    public async Task Handle_WithBeforeCreateValidation_ShouldReturnFailure()
    {
        // Arrange
        var logger = NullLogger<TestCreateProductHandler>.Instance;
        var handler = new TestCreateProductHandler(_repository, logger);
        var command = new CreateProductCommand("", 100m); // Empty name

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("商品名");
    }

    [Fact]
    public async Task Handle_WithAfterCreate_ShouldExecuteAfterCreateLogic()
    {
        // Arrange
        var logger = NullLogger<TestCreateProductHandler>.Instance;
        var handler = new TestCreateProductHandler(_repository, logger);
        var command = new CreateProductCommand("Test Product", 100m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.AfterCreateCalled.Should().BeTrue();
    }

    // Custom handler with hooks
    private class TestCreateProductHandler : CreateEntityHandler<CreateProductCommand, TestProduct, Guid>
    {
        public bool AfterCreateCalled { get; private set; }

        public TestCreateProductHandler(
            IRepository<TestProduct, Guid> repository,
            Microsoft.Extensions.Logging.ILogger logger)
            : base(repository, logger)
        {
        }

        protected override TestProduct CreateEntity(CreateProductCommand command)
        {
            return new TestProduct(Guid.NewGuid(), command.Name, command.Price);
        }

        protected override Task<Result> BeforeCreateAsync(CreateProductCommand command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.Name))
            {
                return Task.FromResult(Result.Fail("商品名は必須です"));
            }
            return Task.FromResult(Result.Success());
        }

        protected override Task AfterCreateAsync(TestProduct entity, CreateProductCommand command, CancellationToken ct)
        {
            AfterCreateCalled = true;
            return Task.CompletedTask;
        }
    }
}
