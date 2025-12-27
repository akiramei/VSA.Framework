using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Handlers.Abstractions;
using VSA.Kernel;

namespace VSA.Handlers.Commands;

/// <summary>
/// 汎用エンティティ更新ハンドラー
///
/// 【機能】
/// - IDでエンティティを取得して更新
/// - 楽観的排他制御（IVersionedCommand使用時）
/// - 更新前後のフック
///
/// 継承時のカスタマイズポイント:
/// - UpdateEntity: エンティティ更新ロジック
/// - BeforeUpdateAsync: 更新前の処理
/// - AfterUpdateAsync: 更新後の処理
/// </summary>
/// <typeparam name="TCommand">更新コマンドの型</typeparam>
/// <typeparam name="TEntity">エンティティの型</typeparam>
/// <typeparam name="TId">識別子の型</typeparam>
public abstract class UpdateEntityHandler<TCommand, TEntity, TId>
    : IRequestHandler<TCommand, Result>
    where TCommand : ICommand<Result>, IEntityCommand<TId>
    where TEntity : AggregateRoot<TId>
{
    protected readonly IRepository<TEntity, TId> Repository;
    protected readonly ILogger Logger;

    protected UpdateEntityHandler(
        IRepository<TEntity, TId> repository,
        ILogger logger)
    {
        Repository = repository;
        Logger = logger;
    }

    public async Task<Result> Handle(TCommand command, CancellationToken ct)
    {
        // エンティティ取得
        var entity = await Repository.GetByIdAsync(command.Id, ct);
        if (entity is null)
        {
            return Result.Fail($"{typeof(TEntity).Name} が見つかりません: {command.Id}");
        }

        // 楽観的排他制御
        if (command is IVersionedCommand<TId> versionedCommand)
        {
            if (entity.Version != versionedCommand.ExpectedVersion)
            {
                return Result.Fail("他のユーザーによって更新されています。最新のデータを取得してください。");
            }
        }

        // 更新前の処理
        var beforeResult = await BeforeUpdateAsync(entity, command, ct);
        if (beforeResult.IsFailure)
        {
            return beforeResult;
        }

        // エンティティ更新
        UpdateEntity(entity, command);

        // リポジトリで更新
        await Repository.UpdateAsync(entity, ct);

        Logger.LogInformation(
            "エンティティを更新しました: {EntityType} Id={EntityId}",
            typeof(TEntity).Name,
            entity.Id);

        // 更新後の処理
        await AfterUpdateAsync(entity, command, ct);

        return Result.Success();
    }

    /// <summary>
    /// エンティティを更新
    /// </summary>
    protected abstract void UpdateEntity(TEntity entity, TCommand command);

    /// <summary>
    /// 更新前の処理（オプション）
    /// </summary>
    protected virtual Task<Result> BeforeUpdateAsync(TEntity entity, TCommand command, CancellationToken ct)
    {
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// 更新後の処理（オプション）
    /// </summary>
    protected virtual Task AfterUpdateAsync(TEntity entity, TCommand command, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// IEntityUpdaterを使用する汎用更新ハンドラー
/// </summary>
public class UpdateEntityWithUpdaterHandler<TCommand, TEntity, TId>
    : IRequestHandler<TCommand, Result>
    where TCommand : ICommand<Result>, IEntityCommand<TId>
    where TEntity : AggregateRoot<TId>
{
    private readonly IRepository<TEntity, TId> _repository;
    private readonly IEntityUpdater<TCommand, TEntity> _updater;
    private readonly ILogger<UpdateEntityWithUpdaterHandler<TCommand, TEntity, TId>> _logger;

    public UpdateEntityWithUpdaterHandler(
        IRepository<TEntity, TId> repository,
        IEntityUpdater<TCommand, TEntity> updater,
        ILogger<UpdateEntityWithUpdaterHandler<TCommand, TEntity, TId>> logger)
    {
        _repository = repository;
        _updater = updater;
        _logger = logger;
    }

    public async Task<Result> Handle(TCommand command, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(command.Id, ct);
        if (entity is null)
        {
            return Result.Fail($"{typeof(TEntity).Name} が見つかりません: {command.Id}");
        }

        // 楽観的排他制御
        if (command is IVersionedCommand<TId> versionedCommand)
        {
            if (entity.Version != versionedCommand.ExpectedVersion)
            {
                return Result.Fail("他のユーザーによって更新されています。最新のデータを取得してください。");
            }
        }

        _updater.Update(entity, command);
        await _repository.UpdateAsync(entity, ct);

        _logger.LogInformation(
            "エンティティを更新しました: {EntityType} Id={EntityId}",
            typeof(TEntity).Name,
            entity.Id);

        return Result.Success();
    }
}
