using FluentAssertions;
using FluentValidation;
using MediatR;
using VSA.Application;
using VSA.Infrastructure.Behaviors;

namespace VSA.Infrastructure.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand("test");
        var callCount = 0;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            callCount++;
            return Task.FromResult(Result.Success("success"));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("success");
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithValidValidator_ShouldCallNext()
    {
        // Arrange
        var validator = new TestCommandValidator(isValid: true);
        var validators = new List<IValidator<TestCommand>> { validator };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand("test");
        var callCount = 0;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            callCount++;
            return Task.FromResult(Result.Success("success"));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithInvalidValidator_ShouldReturnFailure()
    {
        // Arrange
        var validator = new TestCommandValidator(isValid: false, "Name is required");
        var validators = new List<IValidator<TestCommand>> { validator };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand("test");
        var callCount = 0;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            callCount++;
            return Task.FromResult(Result.Success("success"));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Should().Contain("Name is required");
        callCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var validator = new TestCommandValidator(isValid: false, "Name is required", "Price must be positive");
        var validators = new List<IValidator<TestCommand>> { validator };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand("test");
        RequestHandlerDelegate<Result<string>> next = (ct) => Task.FromResult(Result.Success("success"));

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Name is required");
        result.Error.Should().Contain("Price must be positive");
    }

    // Test Validator
    private class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator(bool isValid, params string[] errors)
        {
            if (!isValid)
            {
                foreach (var error in errors)
                {
                    RuleFor(x => x.Name).Must(_ => false).WithMessage(error);
                }
            }
        }
    }
}
