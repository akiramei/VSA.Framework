namespace VSA.Application.Interfaces;

/// <summary>
/// 認可要件を指定する属性
/// </summary>
/// <remarks>
/// 【パターン: 属性による宣言的認可】
///
/// 使用シナリオ:
/// - Command/Queryに認可要件を宣言的に指定
/// - AuthorizationBehaviorがこの属性を読み取って認可チェック
///
/// 使用例:
/// <code>
/// [Authorize(Roles = "Admin,Manager")]
/// public record DeleteProductCommand(Guid ProductId) : ICommand&lt;Result&gt;;
///
/// [Authorize(Policy = "CanEditProduct")]
/// public record UpdateProductCommand(...) : ICommand&lt;Result&gt;;
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeAttribute : Attribute
{
    /// <summary>
    /// 必要なロール（カンマ区切りで複数指定可能、いずれか1つでOK）
    /// </summary>
    public string? Roles { get; set; }

    /// <summary>
    /// 必要なポリシー名
    /// </summary>
    public string? Policy { get; set; }

    /// <summary>
    /// 認証のみ必要（ロール・ポリシー指定なし）
    /// </summary>
    public AuthorizeAttribute()
    {
    }

    /// <summary>
    /// 指定されたポリシーで認可
    /// </summary>
    public AuthorizeAttribute(string policy)
    {
        Policy = policy;
    }
}
