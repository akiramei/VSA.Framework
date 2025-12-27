using System.Linq.Expressions;
using VSA.Kernel;

namespace VSA.Handlers.Abstractions;

/// <summary>
/// リポジトリインターフェース
///
/// 集約ルートの永続化を担当する抽象化
/// EF Coreなどの具体的な実装はユーザー側で行う
/// </summary>
/// <typeparam name="TEntity">集約ルートの型</typeparam>
/// <typeparam name="TId">識別子の型</typeparam>
public interface IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
{
    /// <summary>
    /// IDでエンティティを取得
    /// </summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default);

    /// <summary>
    /// 条件に一致する単一のエンティティを取得
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>
    /// 条件に一致するエンティティのリストを取得
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    /// <summary>
    /// ページング付きでエンティティのリストを取得
    /// </summary>
    Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedListAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    /// <summary>
    /// エンティティを追加
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// エンティティを更新
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// エンティティを削除
    /// </summary>
    Task DeleteAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// 条件に一致するエンティティが存在するか確認
    /// </summary>
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>
    /// 条件に一致するエンティティの件数を取得
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);
}
