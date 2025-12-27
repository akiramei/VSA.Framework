using System.Linq.Expressions;
using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using VSA.Application.Common;
using VSA.Application.Interfaces;
using VSA.Handlers.Abstractions;
using VSA.Kernel;

namespace VSA.Handlers.Queries;

/// <summary>
/// 汎用リスト取得クエリハンドラー（ページング対応）
///
/// 【機能】
/// - ページング付きリスト取得
/// - 検索条件の適用
/// - DTOへのマッピング
/// - 論理削除の除外（オプション）
///
/// 継承時のカスタマイズポイント:
/// - BuildPredicate: 検索条件の構築
/// - MapToDto: エンティティからDTOへの変換
/// - ExcludeDeleted: 論理削除を除外するか（デフォルトtrue）
/// </summary>
/// <typeparam name="TQuery">クエリの型</typeparam>
/// <typeparam name="TEntity">エンティティの型</typeparam>
/// <typeparam name="TId">識別子の型</typeparam>
/// <typeparam name="TDto">DTOの型</typeparam>
public abstract class GetListQueryHandler<TQuery, TEntity, TId, TDto>
    : IRequestHandler<TQuery, Result<PagedResult<TDto>>>
    where TQuery : IQuery<Result<PagedResult<TDto>>>, IPagedQuery
    where TEntity : AggregateRoot<TId>
{
    protected readonly IRepository<TEntity, TId> Repository;
    protected readonly ILogger Logger;

    /// <summary>
    /// 論理削除されたエンティティを除外するかどうか
    /// </summary>
    protected virtual bool ExcludeDeleted => true;

    protected GetListQueryHandler(
        IRepository<TEntity, TId> repository,
        ILogger logger)
    {
        Repository = repository;
        Logger = logger;
    }

    public async Task<Result<PagedResult<TDto>>> Handle(TQuery query, CancellationToken ct)
    {
        // 検索条件を構築
        var predicate = BuildPredicate(query);

        // 論理削除を除外
        if (ExcludeDeleted && typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
        {
            predicate = CombineWithNotDeleted(predicate);
        }

        // ページング取得
        var (items, totalCount) = await Repository.GetPagedListAsync(
            query.PageNumber,
            query.PageSize,
            predicate,
            ct);

        // DTOに変換
        var dtos = items.Select(MapToDto).ToList();

        var result = PagedResult<TDto>.Create(
            dtos,
            totalCount,
            query.PageNumber,
            query.PageSize);

        Logger.LogDebug(
            "リストを取得しました: {EntityType} Count={Count} TotalCount={TotalCount}",
            typeof(TEntity).Name,
            items.Count,
            totalCount);

        return Result.Success(result);
    }

    /// <summary>
    /// 検索条件を構築
    /// </summary>
    protected virtual Expression<Func<TEntity, bool>>? BuildPredicate(TQuery query)
    {
        return null;
    }

    /// <summary>
    /// エンティティをDTOに変換
    /// </summary>
    protected abstract TDto MapToDto(TEntity entity);

    /// <summary>
    /// 論理削除を除外する条件を追加
    /// </summary>
    private static Expression<Func<TEntity, bool>>? CombineWithNotDeleted(
        Expression<Func<TEntity, bool>>? predicate)
    {
        // ISoftDeletable.IsDeleted == false の条件を追加
        Expression<Func<TEntity, bool>> notDeleted = e =>
            !((ISoftDeletable)e).IsDeleted;

        if (predicate is null)
        {
            return notDeleted;
        }

        // 両方の条件を AND で結合
        var parameter = Expression.Parameter(typeof(TEntity), "e");

        var left = ReplaceParameter(predicate.Body, predicate.Parameters[0], parameter);
        var right = ReplaceParameter(notDeleted.Body, notDeleted.Parameters[0], parameter);

        var combined = Expression.AndAlso(left, right);
        return Expression.Lambda<Func<TEntity, bool>>(combined, parameter);
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParam, ParameterExpression newParam)
    {
        return new ParameterReplacer(oldParam, newParam).Visit(expression);
    }

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParam;
        private readonly ParameterExpression _newParam;

        public ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
        {
            _oldParam = oldParam;
            _newParam = newParam;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParam ? _newParam : base.VisitParameter(node);
        }
    }
}

/// <summary>
/// 全件取得クエリハンドラー（ページングなし）
/// </summary>
/// <typeparam name="TQuery">クエリの型</typeparam>
/// <typeparam name="TEntity">エンティティの型</typeparam>
/// <typeparam name="TId">識別子の型</typeparam>
/// <typeparam name="TDto">DTOの型</typeparam>
public abstract class GetAllQueryHandler<TQuery, TEntity, TId, TDto>
    : IRequestHandler<TQuery, Result<IReadOnlyList<TDto>>>
    where TQuery : IQuery<Result<IReadOnlyList<TDto>>>
    where TEntity : AggregateRoot<TId>
{
    protected readonly IRepository<TEntity, TId> Repository;
    protected readonly ILogger Logger;

    /// <summary>
    /// 論理削除されたエンティティを除外するかどうか
    /// </summary>
    protected virtual bool ExcludeDeleted => true;

    protected GetAllQueryHandler(
        IRepository<TEntity, TId> repository,
        ILogger logger)
    {
        Repository = repository;
        Logger = logger;
    }

    public async Task<Result<IReadOnlyList<TDto>>> Handle(TQuery query, CancellationToken ct)
    {
        // 検索条件を構築
        var predicate = BuildPredicate(query);

        // 論理削除を除外
        if (ExcludeDeleted && typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
        {
            predicate = CombineWithNotDeleted(predicate);
        }

        var items = await Repository.GetListAsync(predicate, ct);
        var dtos = items.Select(MapToDto).ToList();

        Logger.LogDebug(
            "全件リストを取得しました: {EntityType} Count={Count}",
            typeof(TEntity).Name,
            items.Count);

        return Result.Success<IReadOnlyList<TDto>>(dtos);
    }

    /// <summary>
    /// 検索条件を構築
    /// </summary>
    protected virtual Expression<Func<TEntity, bool>>? BuildPredicate(TQuery query)
    {
        return null;
    }

    /// <summary>
    /// エンティティをDTOに変換
    /// </summary>
    protected abstract TDto MapToDto(TEntity entity);

    private static Expression<Func<TEntity, bool>>? CombineWithNotDeleted(
        Expression<Func<TEntity, bool>>? predicate)
    {
        Expression<Func<TEntity, bool>> notDeleted = e =>
            !((ISoftDeletable)e).IsDeleted;

        if (predicate is null)
        {
            return notDeleted;
        }

        var parameter = Expression.Parameter(typeof(TEntity), "e");

        var left = ReplaceParameter(predicate.Body, predicate.Parameters[0], parameter);
        var right = ReplaceParameter(notDeleted.Body, notDeleted.Parameters[0], parameter);

        var combined = Expression.AndAlso(left, right);
        return Expression.Lambda<Func<TEntity, bool>>(combined, parameter);
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParam, ParameterExpression newParam)
    {
        return new ParameterReplacer(oldParam, newParam).Visit(expression);
    }

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParam;
        private readonly ParameterExpression _newParam;

        public ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
        {
            _oldParam = oldParam;
            _newParam = newParam;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParam ? _newParam : base.VisitParameter(node);
        }
    }
}
