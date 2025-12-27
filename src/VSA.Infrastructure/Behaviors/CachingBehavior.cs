using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using VSA.Application.Interfaces;
using VSA.Infrastructure.Abstractions;

namespace VSA.Infrastructure.Behaviors;

/// <summary>
/// キャッシュのPipeline Behavior（ICacheableQueryを実装したQuery専用）
///
/// 【Pipeline順序: 350】
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>, ICacheableQuery
{
    private readonly IMemoryCache _cache;
    private readonly ICurrentUserService? _currentUser;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        IMemoryCache cache,
        ILogger<CachingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService? currentUser = null)
    {
        _cache = cache;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // キーに必ずユーザー/テナント情報を含める（キャッシュ誤配信防止）
        var userSegment = _currentUser?.UserId.ToString("N") ?? "anonymous";
        var tenantSegment = _currentUser?.TenantId?.ToString("N") ?? "default";
        var requestSegment = request.GetCacheKey();

        var cacheKey = $"{typeof(TRequest).Name}:{tenantSegment}:{userSegment}:{requestSegment}";

        // キャッシュから取得
        if (_cache.TryGetValue(cacheKey, out TResponse? cached))
        {
            _logger.LogDebug("キャッシュヒット: {CacheKey}", cacheKey);
            return cached!;
        }

        // キャッシュミス: Queryを実行
        _logger.LogDebug("キャッシュミス: {CacheKey}", cacheKey);
        var response = await next();

        // キャッシュに保存
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(request.CacheDurationMinutes)
        };

        _cache.Set(cacheKey, response, cacheOptions);

        _logger.LogDebug(
            "キャッシュ保存: {CacheKey} (有効期限: {Minutes}分)",
            cacheKey,
            request.CacheDurationMinutes);

        return response;
    }
}
