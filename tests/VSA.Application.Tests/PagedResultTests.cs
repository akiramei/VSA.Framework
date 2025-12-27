using VSA.Application.Common;

namespace VSA.Application.Tests;

public class PagedResultTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var items = new List<string> { "A", "B", "C" };

        // Act
        var result = PagedResult<string>.Create(items, 100, 1, 10);

        // Assert
        Assert.Equal(items, result.Items);
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var result = PagedResult<int>.Create(new List<int>(), 95, 1, 10);

        // Assert
        Assert.Equal(10, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WithExactDivision_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var result = PagedResult<int>.Create(new List<int>(), 100, 1, 10);

        // Assert
        Assert.Equal(10, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WithZeroItems_ShouldReturnZero()
    {
        // Arrange & Act
        var result = PagedResult<int>.Create(new List<int>(), 0, 1, 10);

        // Assert
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void HasPreviousPage_OnFirstPage_ShouldReturnFalse()
    {
        // Arrange & Act
        var result = PagedResult<int>.Create(new List<int>(), 100, 1, 10);

        // Assert
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public void HasPreviousPage_OnSecondPage_ShouldReturnTrue()
    {
        // Arrange & Act
        var result = PagedResult<int>.Create(new List<int>(), 100, 2, 10);

        // Assert
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public void HasNextPage_OnLastPage_ShouldReturnFalse()
    {
        // Arrange & Act
        var result = PagedResult<int>.Create(new List<int>(), 100, 10, 10);

        // Assert
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void HasNextPage_OnFirstPage_ShouldReturnTrue()
    {
        // Arrange & Act
        var result = PagedResult<int>.Create(new List<int>(), 100, 1, 10);

        // Assert
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void Empty_ShouldReturnEmptyResult()
    {
        // Act
        var result = PagedResult<string>.Empty(1, 10);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void Empty_WithDefaults_ShouldReturnEmptyResult()
    {
        // Act
        var result = PagedResult<string>.Empty();

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
    }
}
