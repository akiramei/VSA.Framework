namespace VSA.Handlers.Abstractions;

/// <summary>
/// IDでエンティティを取得するクエリのインターフェース
/// </summary>
/// <typeparam name="TId">識別子の型</typeparam>
public interface IGetByIdQuery<out TId>
{
    /// <summary>
    /// 取得対象のエンティティID
    /// </summary>
    TId Id { get; }
}

/// <summary>
/// ページング可能なリストクエリのインターフェース
/// </summary>
public interface IPagedQuery
{
    /// <summary>
    /// ページ番号（1から開始）
    /// </summary>
    int PageNumber { get; }

    /// <summary>
    /// ページサイズ
    /// </summary>
    int PageSize { get; }
}
