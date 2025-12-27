using FluentAssertions;
using VSA.Blazor.State;

namespace VSA.Blazor.Tests;

public class LoadingStateTests
{
    [Fact]
    public void InitialState_ShouldNotBeLoading()
    {
        // Arrange & Act
        var state = new LoadingState();

        // Assert
        state.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void StartLoading_ShouldSetIsLoadingTrue()
    {
        // Arrange
        var state = new LoadingState();

        // Act
        state.StartLoading();

        // Assert
        state.IsLoading.Should().BeTrue();
    }

    [Fact]
    public void StopLoading_ShouldSetIsLoadingFalse()
    {
        // Arrange
        var state = new LoadingState();
        state.StartLoading();

        // Act
        state.StopLoading();

        // Assert
        state.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void MultipleStartLoading_ShouldRequireMultipleStops()
    {
        // Arrange
        var state = new LoadingState();

        // Act
        state.StartLoading();
        state.StartLoading();
        state.StopLoading();

        // Assert - Still loading because we need one more stop
        state.IsLoading.Should().BeTrue();

        // Act
        state.StopLoading();

        // Assert
        state.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithResult_ShouldManageLoadingState()
    {
        // Arrange
        var state = new LoadingState();
        var loadingDuringExecution = false;

        // Act
        var result = await state.ExecuteAsync(async () =>
        {
            loadingDuringExecution = state.IsLoading;
            await Task.Delay(10);
            return 42;
        });

        // Assert
        loadingDuringExecution.Should().BeTrue();
        state.IsLoading.Should().BeFalse();
        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_NoResult_ShouldManageLoadingState()
    {
        // Arrange
        var state = new LoadingState();
        var executed = false;

        // Act
        await state.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            executed = true;
        });

        // Assert
        executed.Should().BeTrue();
        state.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_OnException_ShouldStopLoading()
    {
        // Arrange
        var state = new LoadingState();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await state.ExecuteAsync<int>(() => throw new InvalidOperationException());
        });

        state.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void Reset_ShouldClearLoadingCount()
    {
        // Arrange
        var state = new LoadingState();
        state.StartLoading();
        state.StartLoading();

        // Act
        state.Reset();

        // Assert
        state.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void StopLoading_MoreThanStarted_ShouldNotGoBelowZero()
    {
        // Arrange
        var state = new LoadingState();
        state.StartLoading();

        // Act
        state.StopLoading();
        state.StopLoading();
        state.StopLoading();

        // Assert
        state.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void OnStateChanged_ShouldBeCalled()
    {
        // Arrange
        var state = new LoadingState();
        var callCount = 0;
        state.OnStateChanged = () => callCount++;

        // Act
        state.StartLoading();
        state.StopLoading();

        // Assert
        callCount.Should().Be(2);
    }
}
