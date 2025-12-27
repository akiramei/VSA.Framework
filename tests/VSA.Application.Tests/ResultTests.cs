using VSA.Application;

namespace VSA.Application.Tests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldReturnSuccessResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_ShouldReturnFailureResult()
    {
        // Arrange
        var errorMessage = "Something went wrong";

        // Act
        var result = Result.Fail(errorMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
    }

    [Fact]
    public void Success_WithValue_ShouldReturnSuccessResultWithValue()
    {
        // Arrange
        var value = 42;

        // Act
        var result = Result.Success(value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(value, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_WithType_ShouldReturnFailureResultWithNullValue()
    {
        // Arrange
        var errorMessage = "Not found";

        // Act
        var result = Result.Fail<int>(errorMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(default(int), result.Value);
        Assert.Equal(errorMessage, result.Error);
    }

    [Fact]
    public void Success_WithReferenceType_ShouldReturnSuccessResult()
    {
        // Arrange
        var value = "Hello, World!";

        // Act
        var result = Result.Success(value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Fail_WithReferenceType_ShouldReturnFailureResultWithNullValue()
    {
        // Arrange
        var errorMessage = "Error";

        // Act
        var result = Result.Fail<string>(errorMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Success_WithGuid_ShouldReturnSuccessResult()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = Result.Success(guid);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(guid, result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromResult_ShouldReturnValue()
    {
        // Arrange
        var value = 42;
        var result = Result.Success(value);

        // Act
        int? converted = result;

        // Assert
        Assert.Equal(value, converted);
    }
}
