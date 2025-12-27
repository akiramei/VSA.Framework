using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Handlers.Abstractions;
using VSA.Kernel;

namespace VSA.Handlers.Commands;

/// <summary>
/// 汎用エンティティ削除ハンドラー
///
/// 【機能】
/// - ISoftDeletable実装時は論理削除
/// - それ以外は物理削除
/// - 楽観的排他制御（IVersionedCommand使用時）
///
/// 継承時のカスタマイズポイント:
/// - BeforeDeleteAsync: 削除前の処理（関連チェック等）
/// - AfterDeleteAsync: 削除後の処理（後処理等）
/// </summary>
/// <typeparam name="TCommand">削除コマンドの型</typeparam>
/// <typeparam name="TEntity">エンティティの型</typeparam>
/// <typeparam name="TId">識別子の型</typeparam>
public abstract class DeleteEntityHandler<TCommand, TEntity, TId>
    : IRequestHandler<TCommand, Result>
    where TCommand : ICommand<Result>, IEntityCommand<TId>
    where TEntity : AggregateRoot<TId>
{
    protected readonly IRepository<TEntity, TId> Repository;
    protected readonly ILogger Logger;

    /// <summary>
    /// 論理削除を使用するかどうか
    /// デフォルトはエンティティがISoftDeletableを実装している場合はtrue
    /// </summary>
    protected virtual bool UseSoftDelete => typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity));

    protected DeleteEntityHandler(
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

        // 削除前の処理
        var beforeResult = await BeforeDeleteAsync(entity, command, ct);
        if (beforeResult.IsFailure)
        {
            return beforeResult;
        }

        // 論理削除 or 物理削除
        if (UseSoftDelete && entity is ISoftDeletable softDeletable)
        {
            softDeletable.Delete();
            await Repository.UpdateAsync(entity, ct);

            Logger.LogInformation(
                "エンティティを論理削除しました: {EntityType} Id={EntityId}",
                typeof(TEntity).Name,
                entity.Id);
        }
        else
        {
            await Repository.DeleteAsync(entity, ct);

            Logger.LogInformation(
                "エンティティを物理削除しました: {EntityType} Id={EntityId}",
                typeof(TEntity).Name,
                entity.Id);
        }

        // 削除後の処理
        await AfterDeleteAsync(entity, command, ct);

        return Result.Success();
    }

    /// <summary>
    /// 削除前の処理（オプション）
    /// 関連データのチェックなどに使用
    /// </summary>
    protected virtual Task<Result> BeforeDeleteAsync(TEntity entity, TCommand command, CancellationToken ct)
    {
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// 削除後の処理（オプション）
    /// </summary>
    protected virtual Task AfterDeleteAsync(TEntity entity, TCommand command, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// シンプルな汎用削除ハンドラー
/// 継承不要で使用可能
/// </summary>
public class SimpleDeleteEntityHandler<TCommand, TEntity, TId>
    : IRequestHandler<TCommand, Result>
    where TCommand : ICommand<Result>, IEntityCommand<TId>
    where TEntity : AggregateRoot<TId>
{
    private readonly IRepository<TEntity, TId> _repository;
    private readonly ILogger<SimpleDeleteEntityHandler<TCommand, TEntity, TId>> _logger;

    public SimpleDeleteEntityHandler(
        IRepository<TEntity, TId> repository,
        ILogger<SimpleDeleteEntityHandler<TCommand, TEntity, TId>> logger)
    {
        _repository = repository;
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

        // 論理削除 or 物理削除
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.Delete();
            await _repository.UpdateAsync(entity, ct);

            _logger.LogInformation(
                "エンティティを論理削除しました: {EntityType} Id={EntityId}",
                typeof(TEntity).Name,
                entity.Id);
        }
        else
        {
            await _repository.DeleteAsync(entity, ct);

            _logger.LogInformation(
                "エンティティを物理削除しました: {EntityType} Id={EntityId}",
                typeof(TEntity).Name,
                entity.Id);
        }

        return Result.Success();
    }
}
