using System.Security.Claims;

namespace VSA.Infrastructure.Abstractions;

/// <summary>
/// 認可サービスインターフェース
/// ロールベースおよびポリシーベースの認可をサポート
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// 指定されたロールを持っているか確認
    /// </summary>
    Task<bool> IsInRoleAsync(ClaimsPrincipal user, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定されたポリシーを満たしているか確認
    /// </summary>
    Task<bool> AuthorizeAsync(ClaimsPrincipal user, string policy, CancellationToken cancellationToken = default);
}

/// <summary>
/// 認可結果
/// </summary>
public sealed record AuthorizationResult
{
    /// <summary>
    /// 認可成功
    /// </summary>
    public bool IsAuthorized { get; init; }

    /// <summary>
    /// 失敗理由（認可失敗時）
    /// </summary>
    public string? FailureReason { get; init; }

    public static AuthorizationResult Success() => new() { IsAuthorized = true };

    public static AuthorizationResult Failure(string reason) => new()
    {
        IsAuthorized = false,
        FailureReason = reason
    };
}
