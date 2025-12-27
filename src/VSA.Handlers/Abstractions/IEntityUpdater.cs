namespace VSA.Handlers.Abstractions;

/// <summary>
/// エンティティ更新インターフェース
///
/// コマンドを使用してエンティティを更新する責務を持つ
/// 汎用UpdateEntityHandlerと組み合わせて使用
/// </summary>
/// <typeparam name="TCommand">更新コマンドの型</typeparam>
/// <typeparam name="TEntity">エンティティの型</typeparam>
public interface IEntityUpdater<in TCommand, in TEntity>
{
    /// <summary>
    /// コマンドを使用してエンティティを更新
    /// </summary>
    /// <param name="entity">更新対象のエンティティ</param>
    /// <param name="command">更新コマンド</param>
    void Update(TEntity entity, TCommand command);
}
