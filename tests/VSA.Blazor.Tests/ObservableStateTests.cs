using FluentAssertions;
using VSA.Blazor.State;

namespace VSA.Blazor.Tests;

public class ObservableStateTests
{
    [Fact]
    public void InitialState_ShouldBeEmpty()
    {
        // Arrange & Act
        var state = new ObservableState<string>();

        // Assert
        state.Value.Should().BeNull();
        state.IsLoading.Should().BeFalse();
        state.Error.Should().BeNull();
        state.HasValue.Should().BeFalse();
        state.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_Success_ShouldSetValue()
    {
        // Arrange
        var state = new ObservableState<string>();

        // Act
        await state.LoadAsync(() => Task.FromResult<string?>("Hello"));

        // Assert
        state.Value.Should().Be("Hello");
        state.HasValue.Should().BeTrue();
        state.IsLoading.Should().BeFalse();
        state.Error.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_Failure_ShouldSetError()
    {
        // Arrange
        var state = new ObservableState<string>();
        var errorMessage = "読み込みエラー";
        string? capturedError = null;

        // Act
        await state.LoadAsync(
            () => throw new Exception(errorMessage),
            error => capturedError = error);

        // Assert
        state.Error.Should().Be(errorMessage);
        state.HasError.Should().BeTrue();
        state.Value.Should().BeNull();
        capturedError.Should().Be(errorMessage);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingDuringExecution()
    {
        // Arrange
        var state = new ObservableState<string>();
        var loadingStates = new List<bool>();
        state.OnStateChanged = () => loadingStates.Add(state.IsLoading);

        // Act
        await state.LoadAsync(async () =>
        {
            await Task.Delay(10);
            return "Result";
        });

        // Assert
        loadingStates.Should().Contain(true);
        loadingStates.Last().Should().BeFalse();
    }

    [Fact]
    public void SetValue_ShouldUpdateValueAndClearError()
    {
        // Arrange
        var state = new ObservableState<int>();
        state.SetError("Some error");

        // Act
        state.SetValue(42);

        // Assert
        state.Value.Should().Be(42);
        state.HasValue.Should().BeTrue();
        state.Error.Should().BeNull();
        state.HasError.Should().BeFalse();
    }

    [Fact]
    public void SetError_ShouldUpdateError()
    {
        // Arrange
        var state = new ObservableState<string>();

        // Act
        state.SetError("エラー発生");

        // Assert
        state.Error.Should().Be("エラー発生");
        state.HasError.Should().BeTrue();
    }

    [Fact]
    public void Reset_ShouldClearAllState()
    {
        // Arrange
        var state = new ObservableState<string>();
        state.SetValue("Test");
        state.SetError("Error");

        // Act
        state.Reset();

        // Assert
        state.Value.Should().BeNull();
        state.IsLoading.Should().BeFalse();
        state.Error.Should().BeNull();
    }

    [Fact]
    public void OnStateChanged_ShouldBeCalledOnValueChange()
    {
        // Arrange
        var state = new ObservableState<string>();
        var callCount = 0;
        state.OnStateChanged = () => callCount++;

        // Act
        state.SetValue("Test");

        // Assert
        callCount.Should().BeGreaterThan(0);
    }
}
