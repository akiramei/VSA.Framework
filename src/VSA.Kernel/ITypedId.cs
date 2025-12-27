namespace VSA.Kernel;

/// <summary>
/// 型付きID（TypedId）のマーカーインターフェース
/// readonly record struct で実装することを推奨
/// </summary>
/// <remarks>
/// 【パターン: 型安全なID】
///
/// IDを型で区別することで、異なるエンティティのIDを混同するバグを防止
///
/// 推奨実装例:
/// <code>
/// public readonly record struct BookId(Guid Value) : ITypedId
/// {
///     public static BookId NewId() => new(Guid.NewGuid());
///     public static BookId From(Guid value) => new(value);
/// }
/// </code>
///
/// EF Core設定例:
/// <code>
/// builder.Property(x => x.Id)
///     .HasConversion(
///         id => id.Value,
///         value => new BookId(value));
/// </code>
/// </remarks>
public interface ITypedId
{
    /// <summary>
    /// IDの値（Guid）
    /// </summary>
    Guid Value { get; }
}

/// <summary>
/// ITypedId の拡張メソッド
/// </summary>
public static class TypedIdExtensions
{
    /// <summary>
    /// IDが空かどうかを判定
    /// </summary>
    public static bool IsEmpty<T>(this T id) where T : ITypedId
        => id.Value == Guid.Empty;

    /// <summary>
    /// IDが有効（空でない）かどうかを判定
    /// </summary>
    public static bool IsValid<T>(this T id) where T : ITypedId
        => id.Value != Guid.Empty;
}
