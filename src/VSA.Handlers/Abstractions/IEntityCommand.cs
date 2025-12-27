namespace VSA.Handlers.Abstractions;

/// <summary>
/// エンティティIDを持つコマンドのインターフェース
///
/// 更新・削除コマンドで使用
/// </summary>
/// <typeparam name="TId">識別子の型</typeparam>
public interface IEntityCommand<out TId>
{
    /// <summary>
    /// 対象エンティティのID
    /// </summary>
    TId Id { get; }
}

/// <summary>
/// 楽観的排他制御用のバージョン情報を持つコマンド
/// </summary>
/// <typeparam name="TId">識別子の型</typeparam>
public interface IVersionedCommand<out TId> : IEntityCommand<TId>
{
    /// <summary>
    /// 期待するバージョン番号
    /// </summary>
    long ExpectedVersion { get; }
}
