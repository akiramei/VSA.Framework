using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Handlers.Abstractions;
using VSA.Kernel;

namespace VSA.Handlers.Queries;

/// <summary>
/// 汎用ID検索クエリハンドラー
///
/// 【機能】
/// - IDでエンティティを取得
/// - DTOへのマッピング
/// - 見つからない場合はResult.Fail
///
/// 継承時のカスタマイズポイント:
/// - MapToDto: エンティティからDTOへの変換
/// </summary>
/// <typeparam name="TQuery">クエリの型</typeparam>
/// <typeparam name="TEntity">エンティティの型</typeparam>
/// <typeparam name="TId">識別子の型</typeparam>
/// <typeparam name="TDto">DTOの型</typeparam>
public abstract class GetByIdQueryHandler<TQuery, TEntity, TId, TDto>
    : IRequestHandler<TQuery, Result<TDto>>
    where TQuery : IQuery<Result<TDto>>, IGetByIdQuery<TId>
    where TEntity : AggregateRoot<TId>
{
    protected readonly IRepository<TEntity, TId> Repository;
    protected readonly ILogger Logger;

    protected GetByIdQueryHandler(
        IRepository<TEntity, TId> repository,
        ILogger logger)
    {
        Repository = repository;
        Logger = logger;
    }

    public async Task<Result<TDto>> Handle(TQuery query, CancellationToken ct)
    {
        var entity = await Repository.GetByIdAsync(query.Id, ct);

        if (entity is null)
        {
            Logger.LogWarning(
                "エンティティが見つかりません: {EntityType} Id={EntityId}",
                typeof(TEntity).Name,
                query.Id);

            return Result.Fail<TDto>($"{typeof(TEntity).Name} が見つかりません: {query.Id}");
        }

        // 論理削除されている場合はエラー
        if (entity is ISoftDeletable { IsDeleted: true })
        {
            Logger.LogWarning(
                "削除済みエンティティにアクセス: {EntityType} Id={EntityId}",
                typeof(TEntity).Name,
                query.Id);

            return Result.Fail<TDto>($"{typeof(TEntity).Name} は削除されています: {query.Id}");
        }

        var dto = MapToDto(entity);
        return Result.Success(dto);
    }

    /// <summary>
    /// エンティティをDTOに変換
    /// </summary>
    protected abstract TDto MapToDto(TEntity entity);
}

/// <summary>
/// エンティティをそのまま返す簡易版クエリハンドラー
/// </summary>
public class SimpleGetByIdQueryHandler<TQuery, TEntity, TId>
    : IRequestHandler<TQuery, Result<TEntity>>
    where TQuery : IQuery<Result<TEntity>>, IGetByIdQuery<TId>
    where TEntity : AggregateRoot<TId>
{
    private readonly IRepository<TEntity, TId> _repository;
    private readonly ILogger<SimpleGetByIdQueryHandler<TQuery, TEntity, TId>> _logger;

    public SimpleGetByIdQueryHandler(
        IRepository<TEntity, TId> repository,
        ILogger<SimpleGetByIdQueryHandler<TQuery, TEntity, TId>> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<TEntity>> Handle(TQuery query, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(query.Id, ct);

        if (entity is null)
        {
            _logger.LogWarning(
                "エンティティが見つかりません: {EntityType} Id={EntityId}",
                typeof(TEntity).Name,
                query.Id);

            return Result.Fail<TEntity>($"{typeof(TEntity).Name} が見つかりません: {query.Id}");
        }

        // 論理削除されている場合はエラー
        if (entity is ISoftDeletable { IsDeleted: true })
        {
            return Result.Fail<TEntity>($"{typeof(TEntity).Name} は削除されています: {query.Id}");
        }

        return Result.Success(entity);
    }
}
