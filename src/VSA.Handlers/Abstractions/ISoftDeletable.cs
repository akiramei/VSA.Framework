namespace VSA.Handlers.Abstractions;

/// <summary>
/// 論理削除可能なエンティティのインターフェース
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// 削除済みかどうか
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// 削除日時
    /// </summary>
    DateTimeOffset? DeletedAt { get; }

    /// <summary>
    /// 論理削除を実行
    /// </summary>
    void Delete();

    /// <summary>
    /// 論理削除を取り消し
    /// </summary>
    void Restore();
}
