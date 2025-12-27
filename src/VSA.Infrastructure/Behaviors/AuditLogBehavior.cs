using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Infrastructure.Abstractions;

namespace VSA.Infrastructure.Behaviors;

/// <summary>
/// 監査ログを記録するPipeline Behavior
/// IAuditableCommand を実装したCommandのみ記録対象
/// </summary>
/// <remarks>
/// Pipeline Order: 550（TransactionBehaviorの後、LoggingBehaviorの前）
/// </remarks>
public sealed class AuditLogBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditLogRepository? _auditLogRepository;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ILogger<AuditLogBehavior<TRequest, TResponse>> _logger;

    public AuditLogBehavior(
        ILogger<AuditLogBehavior<TRequest, TResponse>> logger,
        IAuditLogRepository? auditLogRepository = null,
        ICurrentUserService? currentUserService = null)
    {
        _logger = logger;
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // IAuditableCommand を実装していない場合はスキップ
        if (request is not IAuditableCommand auditableCommand)
        {
            return await next();
        }

        // リポジトリが登録されていない場合はスキップ
        if (_auditLogRepository is null)
        {
            _logger.LogDebug(
                "AuditLogRepository が登録されていないため監査ログをスキップします: {RequestType}",
                typeof(TRequest).Name);
            return await next();
        }

        var auditInfo = auditableCommand.GetAuditInfo();
        var timestamp = DateTimeOffset.UtcNow;
        bool isSuccess;
        string? errorMessage = null;

        try
        {
            var response = await next();

            // Result型の場合は成功/失敗を判定
            isSuccess = IsSuccessResponse(response);
            if (!isSuccess && response is Result result)
            {
                errorMessage = result.Error;
            }

            await SaveAuditLogAsync(
                auditInfo,
                request,
                timestamp,
                isSuccess,
                errorMessage,
                cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            // 例外発生時も監査ログを記録
            await SaveAuditLogAsync(
                auditInfo,
                request,
                timestamp,
                isSuccess: false,
                errorMessage: ex.Message,
                cancellationToken);

            throw;
        }
    }

    private async Task SaveAuditLogAsync(
        AuditInfo auditInfo,
        TRequest request,
        DateTimeOffset timestamp,
        bool isSuccess,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var entry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                Action = auditInfo.Action,
                EntityType = auditInfo.EntityType,
                EntityId = auditInfo.EntityId,
                UserId = _currentUserService?.UserId ?? Guid.Empty,
                UserName = _currentUserService?.UserName ?? "Unknown",
                TenantId = _currentUserService?.TenantId,
                Timestamp = timestamp,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                AdditionalData = SerializeToJson(auditInfo.AdditionalData),
                RequestData = SerializeToJson(request)
            };

            await _auditLogRepository!.SaveAsync(entry, cancellationToken);

            _logger.LogInformation(
                "監査ログを記録しました: Action={Action}, EntityType={EntityType}, EntityId={EntityId}, IsSuccess={IsSuccess}",
                auditInfo.Action,
                auditInfo.EntityType,
                auditInfo.EntityId,
                isSuccess);
        }
        catch (Exception ex)
        {
            // 監査ログの保存失敗は警告ログのみ（処理は継続）
            _logger.LogWarning(
                ex,
                "監査ログの保存に失敗しました: Action={Action}, EntityType={EntityType}, EntityId={EntityId}",
                auditInfo.Action,
                auditInfo.EntityType,
                auditInfo.EntityId);
        }
    }

    private static bool IsSuccessResponse(TResponse response)
    {
        return response switch
        {
            Result result => result.IsSuccess,
            _ => true // Result以外の場合は成功とみなす
        };
    }

    private static string? SerializeToJson(object? value)
    {
        if (value is null)
            return null;

        try
        {
            return JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            return null;
        }
    }
}
