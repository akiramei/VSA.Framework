using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VSA.Infrastructure.Behaviors;

namespace VSA.Infrastructure.Extensions;

/// <summary>
/// VSA.Infrastructure の DI 拡張メソッド
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// VSA Framework の全機能を登録
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="assemblies">Handler/Validatorを検索するアセンブリ</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddVsaFramework(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        // MediatR登録
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));

        // FluentValidation登録
        services.AddValidatorsFromAssemblies(assemblies);

        // 標準Behavior登録
        services.AddVsaBehaviors();

        // MemoryCache登録
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// VSA Framework の Behaviors を登録
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="configure">オプション設定</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddVsaBehaviors(
        this IServiceCollection services,
        Action<VsaBehaviorOptions>? configure = null)
    {
        var options = new VsaBehaviorOptions();
        configure?.Invoke(options);

        // Pipeline順序に従って登録（内側から外側へ実行される）
        // 登録順序の逆順で実行される点に注意

        // Order 0: 例外ハンドリング（最外層）
        if (options.EnableExceptionHandling)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
        }

        // Order 50: パフォーマンス監視
        if (options.EnablePerformanceMonitoring)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        }

        // Order 100: バリデーション
        if (options.EnableValidation)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        }

        // Order 200: 認可（[Authorize]属性を持つCommand/Queryのみ）
        if (options.EnableAuthorization)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        }

        // Order 300: 冪等性（IIdempotentCommand を実装したCommandのみ）
        if (options.EnableIdempotency)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        }

        // Order 350: キャッシング（ICacheableQuery を実装したQueryのみ）
        if (options.EnableCaching)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        }

        // Order 400: トランザクション（ICommand を実装したCommandのみ）
        if (options.EnableTransaction)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        }

        // Order 550: 監査ログ（IAuditableCommand を実装したCommandのみ）
        if (options.EnableAuditLog)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLogBehavior<,>));
        }

        // Order 600: ロギング
        if (options.EnableLogging)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        }

        return services;
    }

    /// <summary>
    /// MediatR のみを登録（Behaviorsは含まない）
    /// </summary>
    public static IServiceCollection AddVsaMediatR(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));
        return services;
    }

    /// <summary>
    /// FluentValidation のみを登録
    /// </summary>
    public static IServiceCollection AddVsaValidation(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddValidatorsFromAssemblies(assemblies);
        return services;
    }
}

/// <summary>
/// VSA Behavior のオプション設定
/// </summary>
public class VsaBehaviorOptions
{
    /// <summary>
    /// 例外ハンドリングを有効にする（デフォルト: true）
    /// </summary>
    public bool EnableExceptionHandling { get; set; } = true;

    /// <summary>
    /// パフォーマンス監視を有効にする（デフォルト: true）
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// バリデーションを有効にする（デフォルト: true）
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// キャッシングを有効にする（デフォルト: true）
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// トランザクションを有効にする（デフォルト: true）
    /// </summary>
    public bool EnableTransaction { get; set; } = true;

    /// <summary>
    /// ロギングを有効にする（デフォルト: true）
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// 認可を有効にする（デフォルト: true）
    /// </summary>
    public bool EnableAuthorization { get; set; } = true;

    /// <summary>
    /// 冪等性を有効にする（デフォルト: true）
    /// </summary>
    public bool EnableIdempotency { get; set; } = true;

    /// <summary>
    /// 監査ログを有効にする（デフォルト: true）
    /// </summary>
    public bool EnableAuditLog { get; set; } = true;

    /// <summary>
    /// 遅いリクエストの閾値（ミリ秒）（デフォルト: 500）
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 500;
}
