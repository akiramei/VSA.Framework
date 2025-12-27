namespace VSA.Handlers.Abstractions;

/// <summary>
/// エンティティファクトリインターフェース
///
/// コマンドからエンティティを作成する責務を持つ
/// 汎用CreateEntityHandlerと組み合わせて使用
/// </summary>
/// <typeparam name="TCommand">作成コマンドの型</typeparam>
/// <typeparam name="TEntity">エンティティの型</typeparam>
public interface IEntityFactory<in TCommand, out TEntity>
{
    /// <summary>
    /// コマンドからエンティティを作成
    /// </summary>
    /// <param name="command">作成コマンド</param>
    /// <returns>作成されたエンティティ</returns>
    TEntity Create(TCommand command);
}
