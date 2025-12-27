namespace VSA.Infrastructure.Abstractions;

/// <summary>
/// 冪等性ストアインターフェース
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// 冪等性レコードを取得
    /// </summary>
    Task<IdempotencyRecord?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 冪等性レコードを保存
    /// </summary>
    Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// 冪等性レコードを削除（エラー時のクリーンアップ用）
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// 冪等性レコード
/// </summary>
public sealed record IdempotencyRecord
{
    /// <summary>
    /// 冪等性キー
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// リクエスト型名
    /// </summary>
    public required string RequestType { get; init; }

    /// <summary>
    /// レスポンス（JSON形式）
    /// </summary>
    public required string ResponseJson { get; init; }

    /// <summary>
    /// レスポンス型名
    /// </summary>
    public required string ResponseType { get; init; }

    /// <summary>
    /// 作成日時（UTC）
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 有効期限（UTC）
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// ステータス
    /// </summary>
    public required IdempotencyStatus Status { get; init; }
}

/// <summary>
/// 冪等性レコードのステータス
/// </summary>
public enum IdempotencyStatus
{
    /// <summary>
    /// 処理中
    /// </summary>
    Processing,

    /// <summary>
    /// 完了
    /// </summary>
    Completed,

    /// <summary>
    /// 失敗
    /// </summary>
    Failed
}
