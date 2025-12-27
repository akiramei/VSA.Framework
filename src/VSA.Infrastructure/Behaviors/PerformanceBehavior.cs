using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace VSA.Infrastructure.Behaviors;

/// <summary>
/// パフォーマンス監視のPipeline Behavior
/// 長時間実行のリクエストを警告ログに記録
///
/// 【Pipeline順序: 50】
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly int _slowRequestThresholdMs;

    public PerformanceBehavior(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
        int slowRequestThresholdMs = 500)
    {
        _logger = logger;
        _slowRequestThresholdMs = slowRequestThresholdMs;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;

        if (elapsedMs > _slowRequestThresholdMs)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogWarning(
                "長時間リクエスト: {RequestName} 実行時間: {ElapsedMs}ms (閾値: {Threshold}ms) {@Request}",
                requestName,
                elapsedMs,
                _slowRequestThresholdMs,
                request);
        }

        return response;
    }
}
