using VSA.Application;
using VSA.Application.Common;
using VSA.Application.Interfaces;

namespace VSA.Blazor.Services;

/// <summary>
/// MediatRとResult型を統合したサービスインターフェース。
/// Blazorコンポーネントでのエラーハンドリングを簡素化します。
/// </summary>
public interface IMediatorService
{
    /// <summary>
    /// クエリを実行し、結果を返します。
    /// </summary>
    /// <typeparam name="T">結果の型</typeparam>
    /// <param name="query">実行するクエリ</param>
    /// <param name="onError">エラー時のコールバック（オプション）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>成功時は値、失敗時はdefault</returns>
    Task<T?> QueryAsync<T>(
        IQuery<Result<T>> query,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ページング付きクエリを実行し、結果を返します。
    /// </summary>
    Task<PagedResult<T>?> QueryPagedAsync<T>(
        IQuery<Result<PagedResult<T>>> query,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// コマンドを実行し、結果を返します。
    /// </summary>
    /// <typeparam name="TResult">結果の型</typeparam>
    /// <param name="command">実行するコマンド</param>
    /// <param name="onError">エラー時のコールバック（オプション）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>成功時は値、失敗時はdefault</returns>
    Task<TResult?> CommandAsync<TResult>(
        ICommand<Result<TResult>> command,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 結果を返さないコマンドを実行します。
    /// </summary>
    /// <param name="command">実行するコマンド</param>
    /// <param name="onError">エラー時のコールバック（オプション）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>成功時はtrue、失敗時はfalse</returns>
    Task<bool> CommandAsync(
        ICommand<Result> command,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default);
}
