using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Infrastructure.Abstractions;
using VSA.Infrastructure.Behaviors;

namespace VSA.Infrastructure.Tests.Behaviors;

public class AuthorizationBehaviorTests
{
    [Fact]
    public async Task Handle_WithoutAuthorizeAttribute_ShouldProceed()
    {
        // Arrange
        var logger = NullLogger<AuthorizationBehavior<TestCommand, Result>>.Instance;
        var behavior = new AuthorizationBehavior<TestCommand, Result>(logger);

        var command = new TestCommand("Test");

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAuthorizeAttribute_AndAuthenticatedUser_ShouldProceed()
    {
        // Arrange
        var userService = new TestCurrentUserService(
            Guid.NewGuid(),
            "testuser",
            isAuthenticated: true);
        var logger = NullLogger<AuthorizationBehavior<TestAuthorizedCommand, Result>>.Instance;
        var behavior = new AuthorizationBehavior<TestAuthorizedCommand, Result>(
            logger,
            userService);

        var command = new TestAuthorizedCommand("Test");

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAuthorizeAttribute_AndUnauthenticatedUser_ShouldFail()
    {
        // Arrange
        var userService = new TestCurrentUserService(
            Guid.Empty,
            "",
            isAuthenticated: false);
        var logger = NullLogger<AuthorizationBehavior<TestAuthorizedCommand, Result>>.Instance;
        var behavior = new AuthorizationBehavior<TestAuthorizedCommand, Result>(
            logger,
            userService);

        var command = new TestAuthorizedCommand("Test");

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("認証");
    }

    [Fact]
    public async Task Handle_WithRoleRequirement_AndUserHasRole_ShouldProceed()
    {
        // Arrange
        var userService = new TestCurrentUserService(
            Guid.NewGuid(),
            "admin",
            isAuthenticated: true,
            roles: ["Admin"]);
        var logger = NullLogger<AuthorizationBehavior<TestRoleRequiredCommand, Result>>.Instance;
        var behavior = new AuthorizationBehavior<TestRoleRequiredCommand, Result>(
            logger,
            userService);

        var command = new TestRoleRequiredCommand("Test");

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithRoleRequirement_AndUserMissingRole_ShouldFail()
    {
        // Arrange
        var userService = new TestCurrentUserService(
            Guid.NewGuid(),
            "user",
            isAuthenticated: true,
            roles: ["User"]);
        var logger = NullLogger<AuthorizationBehavior<TestRoleRequiredCommand, Result>>.Instance;
        var behavior = new AuthorizationBehavior<TestRoleRequiredCommand, Result>(
            logger,
            userService);

        var command = new TestRoleRequiredCommand("Test");

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("ロール");
    }

    [Fact]
    public async Task Handle_WithMultipleRoles_AndUserHasOne_ShouldProceed()
    {
        // Arrange
        var userService = new TestCurrentUserService(
            Guid.NewGuid(),
            "manager",
            isAuthenticated: true,
            roles: ["Manager"]);
        var logger = NullLogger<AuthorizationBehavior<TestMultiRoleCommand, Result>>.Instance;
        var behavior = new AuthorizationBehavior<TestMultiRoleCommand, Result>(
            logger,
            userService);

        var command = new TestMultiRoleCommand("Test");

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithGenericResult_AndUnauthorized_ShouldReturnFailedResult()
    {
        // Arrange
        var userService = new TestCurrentUserService(
            Guid.Empty,
            "",
            isAuthenticated: false);
        var logger = NullLogger<AuthorizationBehavior<TestAuthorizedCommandWithValue, Result<Guid>>>.Instance;
        var behavior = new AuthorizationBehavior<TestAuthorizedCommandWithValue, Result<Guid>>(
            logger,
            userService);

        var command = new TestAuthorizedCommandWithValue("Test");

        // Act
        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult(Result.Success(Guid.NewGuid())),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("認証");
    }

    // Test Commands
    private record TestCommand(string Name) : ICommand<Result>;

    [Authorize]
    private record TestAuthorizedCommand(string Name) : ICommand<Result>;

    [Authorize(Roles = "Admin")]
    private record TestRoleRequiredCommand(string Name) : ICommand<Result>;

    [Authorize(Roles = "Admin,Manager")]
    private record TestMultiRoleCommand(string Name) : ICommand<Result>;

    [Authorize]
    private record TestAuthorizedCommandWithValue(string Name) : ICommand<Result<Guid>>;
}
