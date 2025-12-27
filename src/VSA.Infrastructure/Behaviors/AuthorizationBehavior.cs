using System.Reflection;
using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Infrastructure.Abstractions;

namespace VSA.Infrastructure.Behaviors;

/// <summary>
/// 認可をチェックするPipeline Behavior
/// [Authorize]属性を持つCommand/Queryのみ認可チェック
/// </summary>
/// <remarks>
/// Pipeline Order: 200（ValidationBehaviorの後、IdempotencyBehaviorの前）
/// </remarks>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService? _currentUserService;
    private readonly IAuthorizationService? _authorizationService;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public AuthorizationBehavior(
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger,
        ICurrentUserService? currentUserService = null,
        IAuthorizationService? authorizationService = null)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // [Authorize]属性を取得
        var authorizeAttributes = typeof(TRequest)
            .GetCustomAttributes<AuthorizeAttribute>(true)
            .ToList();

        // 属性がない場合はスキップ
        if (authorizeAttributes.Count == 0)
        {
            return await next();
        }

        // ユーザーサービスが登録されていない場合はスキップ
        if (_currentUserService is null)
        {
            _logger.LogDebug(
                "CurrentUserService が登録されていないため認可チェックをスキップします: {RequestType}",
                typeof(TRequest).Name);
            return await next();
        }

        // 認証チェック
        if (!_currentUserService.IsAuthenticated)
        {
            _logger.LogWarning(
                "認証されていないユーザーからのリクエストを拒否: {RequestType}",
                typeof(TRequest).Name);
            return CreateUnauthorizedResponse("認証が必要です");
        }

        // 各属性の認可要件をチェック
        foreach (var attribute in authorizeAttributes)
        {
            var result = await CheckAuthorizationAsync(attribute, cancellationToken);
            if (!result.IsAuthorized)
            {
                _logger.LogWarning(
                    "認可チェック失敗: {RequestType}, Reason={Reason}, UserId={UserId}",
                    typeof(TRequest).Name,
                    result.FailureReason,
                    _currentUserService.UserId);
                return CreateUnauthorizedResponse(result.FailureReason ?? "アクセスが拒否されました");
            }
        }

        _logger.LogDebug(
            "認可チェック成功: {RequestType}, UserId={UserId}",
            typeof(TRequest).Name,
            _currentUserService.UserId);

        return await next();
    }

    private async Task<AuthorizationResult> CheckAuthorizationAsync(
        AuthorizeAttribute attribute,
        CancellationToken cancellationToken)
    {
        // ロールチェック
        if (!string.IsNullOrWhiteSpace(attribute.Roles))
        {
            var roles = attribute.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var hasRole = false;

            foreach (var role in roles)
            {
                if (_authorizationService is not null && _currentUserService?.User is not null)
                {
                    if (await _authorizationService.IsInRoleAsync(_currentUserService.User, role, cancellationToken))
                    {
                        hasRole = true;
                        break;
                    }
                }
                else if (_currentUserService?.IsInRole(role) == true)
                {
                    hasRole = true;
                    break;
                }
            }

            if (!hasRole)
            {
                return AuthorizationResult.Failure($"必要なロールがありません: {attribute.Roles}");
            }
        }

        // ポリシーチェック
        if (!string.IsNullOrWhiteSpace(attribute.Policy))
        {
            if (_authorizationService is null || _currentUserService?.User is null)
            {
                _logger.LogDebug(
                    "AuthorizationService が登録されていないためポリシーチェックをスキップ: {Policy}",
                    attribute.Policy);
                return AuthorizationResult.Success();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                _currentUserService.User,
                attribute.Policy,
                cancellationToken);

            if (!isAuthorized)
            {
                return AuthorizationResult.Failure($"ポリシー '{attribute.Policy}' を満たしていません");
            }
        }

        return AuthorizationResult.Success();
    }

    private static TResponse CreateUnauthorizedResponse(string message)
    {
        // TResponse が Result<T> または Result の場合
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Fail(message);
        }

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var genericArg = typeof(TResponse).GetGenericArguments()[0];
            // Get the generic Fail<T> method
            var failMethod = typeof(Result)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == nameof(Result.Fail) && m.IsGenericMethod)
                .FirstOrDefault()
                ?.MakeGenericMethod(genericArg);

            if (failMethod is not null)
            {
                return (TResponse)failMethod.Invoke(null, [message])!;
            }
        }

        // Result系以外の場合は例外をスロー
        throw new UnauthorizedAccessException(message);
    }
}
