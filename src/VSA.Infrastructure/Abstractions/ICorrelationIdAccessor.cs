namespace VSA.Infrastructure.Abstractions;

/// <summary>
/// リクエストの相関IDを提供するアクセサ
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// 相関ID（リクエストを追跡するためのID）
    /// </summary>
    string CorrelationId { get; }
}
