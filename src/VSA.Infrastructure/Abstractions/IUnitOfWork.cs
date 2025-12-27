namespace VSA.Infrastructure.Abstractions;

/// <summary>
/// Unit of Workパターンのインターフェース
/// トランザクション管理を抽象化
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// 変更をコミット
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// トランザクションを開始
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// トランザクションをコミット
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// トランザクションをロールバック
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
