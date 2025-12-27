using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Handlers.Abstractions;
using VSA.Kernel;

namespace VSA.Handlers.Commands;

/// <summary>
/// 汎用エンティティ作成ハンドラー
///
/// 【使用方法】
/// Option A: IEntityFactoryを実装して自動登録
/// Option B: このクラスを継承してCreateEntityをオーバーライド
///
/// 継承時のカスタマイズポイント:
/// - CreateEntity: エンティティ作成ロジック
/// - BeforeCreateAsync: 作成前の処理（バリデーション等）
/// - AfterCreateAsync: 作成後の処理（通知等）
/// </summary>
/// <typeparam name="TCommand">作成コマンドの型</typeparam>
/// <typeparam name="TEntity">エンティティの型</typeparam>
/// <typeparam name="TId">識別子の型</typeparam>
public abstract class CreateEntityHandler<TCommand, TEntity, TId>
    : IRequestHandler<TCommand, Result<TId>>
    where TCommand : ICommand<Result<TId>>
    where TEntity : AggregateRoot<TId>
{
    protected readonly IRepository<TEntity, TId> Repository;
    protected readonly ILogger Logger;

    protected CreateEntityHandler(
        IRepository<TEntity, TId> repository,
        ILogger logger)
    {
        Repository = repository;
        Logger = logger;
    }

    public async Task<Result<TId>> Handle(TCommand command, CancellationToken ct)
    {
        // 作成前の処理
        var beforeResult = await BeforeCreateAsync(command, ct);
        if (beforeResult.IsFailure)
        {
            return Result.Fail<TId>(beforeResult.Error!);
        }

        // エンティティ作成
        var entity = CreateEntity(command);

        // リポジトリに追加
        await Repository.AddAsync(entity, ct);

        Logger.LogInformation(
            "エンティティを作成しました: {EntityType} Id={EntityId}",
            typeof(TEntity).Name,
            entity.Id);

        // 作成後の処理
        await AfterCreateAsync(entity, command, ct);

        return Result.Success(entity.Id);
    }

    /// <summary>
    /// コマンドからエンティティを作成
    /// </summary>
    protected abstract TEntity CreateEntity(TCommand command);

    /// <summary>
    /// 作成前の処理（オプション）
    /// バリデーションや事前チェックに使用
    /// </summary>
    protected virtual Task<Result> BeforeCreateAsync(TCommand command, CancellationToken ct)
    {
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// 作成後の処理（オプション）
    /// 通知やイベント発行に使用
    /// </summary>
    protected virtual Task AfterCreateAsync(TEntity entity, TCommand command, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// IEntityFactoryを使用する汎用作成ハンドラー
///
/// DIコンテナにIEntityFactoryが登録されている場合に使用
/// </summary>
public class CreateEntityWithFactoryHandler<TCommand, TEntity, TId>
    : IRequestHandler<TCommand, Result<TId>>
    where TCommand : ICommand<Result<TId>>
    where TEntity : AggregateRoot<TId>
{
    private readonly IRepository<TEntity, TId> _repository;
    private readonly IEntityFactory<TCommand, TEntity> _factory;
    private readonly ILogger<CreateEntityWithFactoryHandler<TCommand, TEntity, TId>> _logger;

    public CreateEntityWithFactoryHandler(
        IRepository<TEntity, TId> repository,
        IEntityFactory<TCommand, TEntity> factory,
        ILogger<CreateEntityWithFactoryHandler<TCommand, TEntity, TId>> logger)
    {
        _repository = repository;
        _factory = factory;
        _logger = logger;
    }

    public async Task<Result<TId>> Handle(TCommand command, CancellationToken ct)
    {
        var entity = _factory.Create(command);
        await _repository.AddAsync(entity, ct);

        _logger.LogInformation(
            "エンティティを作成しました: {EntityType} Id={EntityId}",
            typeof(TEntity).Name,
            entity.Id);

        return Result.Success(entity.Id);
    }
}
