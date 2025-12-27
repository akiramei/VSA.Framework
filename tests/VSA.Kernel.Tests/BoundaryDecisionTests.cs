using FluentAssertions;
using VSA.Kernel;

namespace VSA.Kernel.Tests;

public class BoundaryDecisionTests
{
    [Fact]
    public void Allow_ShouldCreateAllowedDecision()
    {
        // Act
        var decision = BoundaryDecision.Allow();

        // Assert
        decision.IsAllowed.Should().BeTrue();
        decision.Reason.Should().BeNull();
    }

    [Fact]
    public void Deny_ShouldCreateDeniedDecisionWithReason()
    {
        // Act
        var decision = BoundaryDecision.Deny("操作は許可されていません");

        // Assert
        decision.IsAllowed.Should().BeFalse();
        decision.Reason.Should().Be("操作は許可されていません");
    }

    [Fact]
    public void ImplicitBoolConversion_AllowedDecision_ShouldBeTrue()
    {
        // Arrange
        var decision = BoundaryDecision.Allow();

        // Act & Assert
        bool result = decision;
        result.Should().BeTrue();
    }

    [Fact]
    public void ImplicitBoolConversion_DeniedDecision_ShouldBeFalse()
    {
        // Arrange
        var decision = BoundaryDecision.Deny("拒否");

        // Act & Assert
        bool result = decision;
        result.Should().BeFalse();
    }

    [Fact]
    public void CanBeUsedInIfStatement_Allowed()
    {
        // Arrange
        var decision = BoundaryDecision.Allow();
        var executed = false;

        // Act
        if (decision)
        {
            executed = true;
        }

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void CanBeUsedInIfStatement_Denied()
    {
        // Arrange
        var decision = BoundaryDecision.Deny("理由");
        var executed = false;

        // Act
        if (!decision)
        {
            executed = true;
        }

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var decision1 = BoundaryDecision.Allow();
        var decision2 = BoundaryDecision.Allow();

        // Assert
        decision1.Should().Be(decision2);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var decision1 = BoundaryDecision.Allow();
        var decision2 = BoundaryDecision.Deny("理由");

        // Assert
        decision1.Should().NotBe(decision2);
    }

    [Fact]
    public void UseCase_EntityValidation()
    {
        // Arrange - Simulate entity validation
        var isActive = true;
        var hasActiveLoans = true;

        // Act - Simulate CanDeactivate logic
        BoundaryDecision CanDeactivate()
        {
            if (!isActive)
                return BoundaryDecision.Deny("既に無効化されています");

            if (hasActiveLoans)
                return BoundaryDecision.Deny("貸出中の本は無効化できません");

            return BoundaryDecision.Allow();
        }

        var decision = CanDeactivate();

        // Assert
        decision.IsAllowed.Should().BeFalse();
        decision.Reason.Should().Be("貸出中の本は無効化できません");
    }

    [Fact]
    public void UseCase_MultipleConditions()
    {
        // Arrange
        var conditions = new[]
        {
            BoundaryDecision.Allow(),
            BoundaryDecision.Allow(),
            BoundaryDecision.Allow()
        };

        // Act - All conditions must pass
        var allAllowed = conditions.All(d => d.IsAllowed);

        // Assert
        allAllowed.Should().BeTrue();
    }

    [Fact]
    public void UseCase_FirstDeniedCondition()
    {
        // Arrange
        var conditions = new[]
        {
            BoundaryDecision.Allow(),
            BoundaryDecision.Deny("条件2が失敗"),
            BoundaryDecision.Allow()
        };

        // Act - Find first denied
        var firstDenied = conditions.FirstOrDefault(d => !d.IsAllowed);

        // Assert
        firstDenied.Reason.Should().Be("条件2が失敗");
    }
}
