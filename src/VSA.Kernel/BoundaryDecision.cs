namespace VSA.Kernel;

/// <summary>
/// ドメイン操作の境界判定結果
/// エンティティの操作可否を明示的に表現
/// </summary>
/// <remarks>
/// 【パターン: Boundary Decision】
///
/// 使用シナリオ:
/// - エンティティの状態変更が許可されるかを事前判定
/// - CanXxx() メソッドの戻り値として使用
/// - 許可されない理由を呼び出し元に伝達
///
/// 使用例:
/// <code>
/// public class Book : AggregateRoot&lt;BookId&gt;
/// {
///     public BoundaryDecision CanDeactivate()
///     {
///         if (!IsActive)
///             return BoundaryDecision.Deny("既に無効化されています");
///
///         if (HasActiveLoans)
///             return BoundaryDecision.Deny("貸出中の本は無効化できません");
///
///         return BoundaryDecision.Allow();
///     }
///
///     public void Deactivate()
///     {
///         var decision = CanDeactivate();
///         if (!decision)
///             throw new DomainException(decision.Reason!);
///
///         IsActive = false;
///     }
/// }
/// </code>
///
/// ハンドラーでの使用例:
/// <code>
/// var decision = book.CanDeactivate();
/// if (!decision)
///     return Result.Fail(decision.Reason!);
/// </code>
/// </remarks>
/// <param name="IsAllowed">操作が許可されるかどうか</param>
/// <param name="Reason">許可されない理由（IsAllowed が false の場合）</param>
public readonly record struct BoundaryDecision(bool IsAllowed, string? Reason)
{
    /// <summary>
    /// 操作を許可
    /// </summary>
    public static BoundaryDecision Allow() => new(true, null);

    /// <summary>
    /// 操作を拒否（理由付き）
    /// </summary>
    /// <param name="reason">拒否理由</param>
    public static BoundaryDecision Deny(string reason) => new(false, reason);

    /// <summary>
    /// bool への暗黙的変換（if文で使用可能）
    /// </summary>
    public static implicit operator bool(BoundaryDecision decision) => decision.IsAllowed;
}
