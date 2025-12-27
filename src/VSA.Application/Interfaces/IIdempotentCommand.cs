namespace VSA.Application.Interfaces;

/// <summary>
/// 冪等性を持つCommandマーカーインターフェース
/// </summary>
/// <remarks>
/// 【パターン: 冪等性保証】
///
/// 使用シナリオ:
/// - 同じリクエストが複数回送信されても同じ結果を返す必要がある場合
/// - ネットワーク障害によるリトライ対策
/// - 決済処理など重複実行を防ぎたい場合
///
/// 実装ガイド:
/// - IdempotencyKeyプロパティでリクエストを一意に識別
/// - クライアントがIdempotencyKeyを生成して送信
/// - 同じKeyのリクエストは保存された結果を返す
///
/// 使用例:
/// <code>
/// public record CreateOrderCommand(
///     string IdempotencyKey,
///     Guid CustomerId,
///     List&lt;OrderItem&gt; Items
/// ) : ICommand&lt;Result&lt;Guid&gt;&gt;, IIdempotentCommand
/// {
///     string IIdempotentCommand.IdempotencyKey =&gt; IdempotencyKey;
/// }
/// </code>
/// </remarks>
public interface IIdempotentCommand
{
    /// <summary>
    /// 冪等性キー（リクエストを一意に識別）
    /// </summary>
    string IdempotencyKey { get; }
}
