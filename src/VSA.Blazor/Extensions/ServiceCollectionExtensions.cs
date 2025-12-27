using Microsoft.Extensions.DependencyInjection;
using VSA.Blazor.Services;

namespace VSA.Blazor.Extensions;

/// <summary>
/// IServiceCollectionの拡張メソッド。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// VSA.Blazorサービスを登録します。
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddVsaBlazor(this IServiceCollection services)
    {
        services.AddScoped<IMediatorService, MediatorService>();

        return services;
    }
}
