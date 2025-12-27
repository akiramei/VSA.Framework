using MediatR;
using VSA.Application;
using VSA.Application.Common;
using VSA.Application.Interfaces;

namespace VSA.Blazor.Services;

/// <summary>
/// IMediatorServiceの実装。
/// MediatRを使用してクエリとコマンドを実行し、Result型を処理します。
/// </summary>
public class MediatorService : IMediatorService
{
    private readonly IMediator _mediator;
    private const string DefaultErrorMessage = "エラーが発生しました";

    public MediatorService(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <inheritdoc />
    public async Task<T?> QueryAsync<T>(
        IQuery<Result<T>> query,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return result.Value;
        }

        onError?.Invoke(result.Error ?? DefaultErrorMessage);
        return default;
    }

    /// <inheritdoc />
    public async Task<PagedResult<T>?> QueryPagedAsync<T>(
        IQuery<Result<PagedResult<T>>> query,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return result.Value;
        }

        onError?.Invoke(result.Error ?? DefaultErrorMessage);
        return default;
    }

    /// <inheritdoc />
    public async Task<TResult?> CommandAsync<TResult>(
        ICommand<Result<TResult>> command,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return result.Value;
        }

        onError?.Invoke(result.Error ?? DefaultErrorMessage);
        return default;
    }

    /// <inheritdoc />
    public async Task<bool> CommandAsync(
        ICommand<Result> command,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return true;
        }

        onError?.Invoke(result.Error ?? DefaultErrorMessage);
        return false;
    }
}
