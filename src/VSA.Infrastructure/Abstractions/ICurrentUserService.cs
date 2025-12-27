using System.Security.Claims;

namespace VSA.Infrastructure.Abstractions;

/// <summary>
/// 現在のユーザー情報を提供するサービス
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// ユーザーID
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// ユーザー名
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// テナントID（マルチテナント環境の場合）
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// 認証済みかどうか
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// ClaimsPrincipal（ASP.NET Core認可サービス用）
    /// </summary>
    ClaimsPrincipal? User { get; }

    /// <summary>
    /// 指定されたロールを持っているかどうか
    /// </summary>
    bool IsInRole(string role);
}
