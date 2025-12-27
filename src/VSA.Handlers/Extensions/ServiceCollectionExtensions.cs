using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Handlers.Abstractions;
using VSA.Handlers.Commands;
using VSA.Handlers.Queries;
using VSA.Kernel;

namespace VSA.Handlers.Extensions;

/// <summary>
/// VSA.HandlersのDI拡張メソッド
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 汎用ハンドラーを登録
    ///
    /// アセンブリをスキャンして以下を自動登録:
    /// - IEntityFactory実装 → CreateEntityWithFactoryHandler
    /// - IEntityUpdater実装 → UpdateEntityWithUpdaterHandler
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="assemblies">スキャン対象のアセンブリ</param>
    public static IServiceCollection AddVsaHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        // IEntityFactory実装を探して登録
        foreach (var assembly in assemblies)
        {
            RegisterEntityFactories(services, assembly);
            RegisterEntityUpdaters(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// 特定のエンティティ用の汎用作成ハンドラーを登録
    /// </summary>
    public static IServiceCollection AddCreateHandler<TCommand, TEntity, TId>(
        this IServiceCollection services)
        where TCommand : ICommand<Result<TId>>
        where TEntity : AggregateRoot<TId>
    {
        services.AddTransient<
            IRequestHandler<TCommand, Result<TId>>,
            CreateEntityWithFactoryHandler<TCommand, TEntity, TId>>();

        return services;
    }

    /// <summary>
    /// 特定のエンティティ用の汎用更新ハンドラーを登録
    /// </summary>
    public static IServiceCollection AddUpdateHandler<TCommand, TEntity, TId>(
        this IServiceCollection services)
        where TCommand : ICommand<Result>, IEntityCommand<TId>
        where TEntity : AggregateRoot<TId>
    {
        services.AddTransient<
            IRequestHandler<TCommand, Result>,
            UpdateEntityWithUpdaterHandler<TCommand, TEntity, TId>>();

        return services;
    }

    /// <summary>
    /// 特定のエンティティ用の汎用削除ハンドラーを登録
    /// </summary>
    public static IServiceCollection AddDeleteHandler<TCommand, TEntity, TId>(
        this IServiceCollection services)
        where TCommand : ICommand<Result>, IEntityCommand<TId>
        where TEntity : AggregateRoot<TId>
    {
        services.AddTransient<
            IRequestHandler<TCommand, Result>,
            SimpleDeleteEntityHandler<TCommand, TEntity, TId>>();

        return services;
    }

    /// <summary>
    /// 特定のエンティティ用の汎用ID検索ハンドラーを登録
    /// </summary>
    public static IServiceCollection AddGetByIdHandler<TQuery, TEntity, TId>(
        this IServiceCollection services)
        where TQuery : IQuery<Result<TEntity>>, IGetByIdQuery<TId>
        where TEntity : AggregateRoot<TId>
    {
        services.AddTransient<
            IRequestHandler<TQuery, Result<TEntity>>,
            SimpleGetByIdQueryHandler<TQuery, TEntity, TId>>();

        return services;
    }

    private static void RegisterEntityFactories(IServiceCollection services, Assembly assembly)
    {
        var factoryTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IEntityFactory<,>)));

        foreach (var factoryType in factoryTypes)
        {
            var interfaces = factoryType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(IEntityFactory<,>));

            foreach (var @interface in interfaces)
            {
                services.AddTransient(@interface, factoryType);
            }
        }
    }

    private static void RegisterEntityUpdaters(IServiceCollection services, Assembly assembly)
    {
        var updaterTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IEntityUpdater<,>)));

        foreach (var updaterType in updaterTypes)
        {
            var interfaces = updaterType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(IEntityUpdater<,>));

            foreach (var @interface in interfaces)
            {
                services.AddTransient(@interface, updaterType);
            }
        }
    }
}
