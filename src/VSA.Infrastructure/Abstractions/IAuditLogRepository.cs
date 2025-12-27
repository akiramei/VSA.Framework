namespace VSA.Infrastructure.Abstractions;

/// <summary>
/// 監査ログのリポジトリインターフェース
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// 監査ログエントリを保存
    /// </summary>
    Task SaveAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>
/// 監査ログエントリ
/// </summary>
public sealed record AuditLogEntry
{
    /// <summary>
    /// ログID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// アクション名（例: CreateProduct, UpdateProduct, DeleteProduct）
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// エンティティ型（例: Product, Order）
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// エンティティID
    /// </summary>
    public required string EntityId { get; init; }

    /// <summary>
    /// 実行したユーザーID
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// 実行したユーザー名
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// テナントID（マルチテナント環境の場合）
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// 実行日時（UTC）
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// 成功したかどうか
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// エラーメッセージ（失敗時）
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 追加データ（JSON形式）
    /// </summary>
    public string? AdditionalData { get; init; }

    /// <summary>
    /// リクエストデータ（JSON形式）
    /// </summary>
    public string? RequestData { get; init; }
}
