namespace VSA.Kernel;

/// <summary>
/// 型付きID（TypedId）の基底クラス（非推奨）
/// エンティティの識別子を型安全に表現する値オブジェクト
/// </summary>
/// <typeparam name="TSelf">派生クラスの型（CRTP パターン）</typeparam>
/// <remarks>
/// このクラスは非推奨です。代わりに readonly record struct で ITypedId を実装してください。
///
/// 推奨パターン:
/// <code>
/// public readonly record struct ProductId(Guid Value) : ITypedId
/// {
///     public static ProductId NewId() => new(Guid.NewGuid());
///     public static ProductId From(Guid value) => new(value);
/// }
/// </code>
/// </remarks>
[Obsolete("readonly record struct で ITypedId を実装してください。例: public readonly record struct ProductId(Guid Value) : ITypedId")]
public abstract class TypedId<TSelf> : ValueObject, IComparable<TSelf>, ITypedId
    where TSelf : TypedId<TSelf>
{
    /// <summary>
    /// IDの値（Guid）
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// TypedId を構築
    /// </summary>
    /// <param name="value">Guid値</param>
    /// <exception cref="ArgumentException">空のGuidが指定された場合</exception>
    protected TypedId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                $"{typeof(TSelf).Name}は空にできません",
                nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// 文字列からIDを生成
    /// </summary>
    /// <param name="value">Guid文字列</param>
    /// <returns>パース結果とID</returns>
    public static bool TryParse(string? value, out TSelf? result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Guid.TryParse(value, out var guid))
            return false;

        if (guid == Guid.Empty)
            return false;

        try
        {
            // Activator を使用してインスタンス生成
            result = (TSelf)Activator.CreateInstance(typeof(TSelf), guid)!;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// Guid への暗黙的変換
    /// </summary>
    public static implicit operator Guid(TypedId<TSelf> id) => id.Value;

    /// <inheritdoc />
    public int CompareTo(TSelf? other)
    {
        if (other is null)
            return 1;

        return Value.CompareTo(other.Value);
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
