using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Infrastructure.Abstractions;

namespace VSA.Infrastructure.Behaviors;

/// <summary>
/// トランザクション管理のPipeline Behavior（Command専用）
///
/// 【パターン: Pipeline Behavior - Transaction】
///
/// 使用シナリオ:
/// - Command実行を単一トランザクションで実行
/// - エラー時に自動ロールバック
/// - Handler内でSaveChangesAsyncを呼ばなくてよい
///
/// 【Pipeline順序: 400】
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : Result
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        try
        {
            _logger.LogDebug("トランザクション開始: {RequestName}", requestName);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var response = await next();

            // 成功時のみコミット
            if (response.IsSuccess)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogDebug("トランザクションコミット: {RequestName}", requestName);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                _logger.LogDebug("トランザクションロールバック（ビジネスエラー）: {RequestName}", requestName);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "トランザクションロールバック（例外）: {RequestName}", requestName);

            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            throw;
        }
    }
}
