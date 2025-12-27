namespace VSA.Kernel;

/// <summary>
/// マルチテナント対応エンティティのマーカーインターフェース
///
/// 【パターン: Multi-Tenant Entity Pattern】
///
/// 責務:
/// - マルチテナント対応が必要なエンティティを識別
/// - TenantIdプロパティの標準化
/// - Global Query Filterの自動適用対象として使用
///
/// 使用例:
/// <code>
/// public sealed class PurchaseRequest : Entity, IMultiTenant
/// {
///     public Guid TenantId { get; private set; }
///
///     public static PurchaseRequest Create(..., Guid tenantId)
///     {
///         return new PurchaseRequest { TenantId = tenantId, ... };
///     }
/// }
/// </code>
///
/// Global Query Filter適用例:
/// <code>
/// modelBuilder.Entity&lt;PurchaseRequest&gt;()
///     .HasQueryFilter(e => e.TenantId == appContext.TenantId);
/// </code>
/// </summary>
public interface IMultiTenant
{
    /// <summary>
    /// テナントID
    /// </summary>
    Guid TenantId { get; }
}
