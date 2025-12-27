using System.Security.Claims;
using VSA.Infrastructure.Abstractions;

namespace VSA.Infrastructure.Tests;

/// <summary>
/// テスト用のユーザーサービス実装
/// </summary>
public class TestCurrentUserService : ICurrentUserService
{
    private readonly string[] _roles;

    public TestCurrentUserService(
        Guid userId,
        string userName,
        bool isAuthenticated,
        Guid? tenantId = null,
        string[]? roles = null)
    {
        UserId = userId;
        UserName = userName;
        IsAuthenticated = isAuthenticated;
        TenantId = tenantId;
        _roles = roles ?? [];

        if (isAuthenticated)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, userName)
            };

            foreach (var role in _roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "Test");
            User = new ClaimsPrincipal(identity);
        }
    }

    public Guid UserId { get; }
    public string UserName { get; }
    public Guid? TenantId { get; }
    public bool IsAuthenticated { get; }
    public ClaimsPrincipal? User { get; }

    public bool IsInRole(string role) => _roles.Contains(role);
}
