using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Infrastructure.Abstractions;

namespace VSA.Infrastructure.Behaviors;

/// <summary>
/// 冪等性を保証するPipeline Behavior
/// IIdempotentCommand を実装したCommandのみ冪等性チェック
/// </summary>
/// <remarks>
/// Pipeline Order: 300（AuthorizationBehaviorの後、CachingBehaviorの前）
/// </remarks>
public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IIdempotencyStore? _idempotencyStore;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;
    private readonly IdempotencyOptions _options;

    public IdempotencyBehavior(
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger,
        IIdempotencyStore? idempotencyStore = null,
        IdempotencyOptions? options = null)
    {
        _logger = logger;
        _idempotencyStore = idempotencyStore;
        _options = options ?? new IdempotencyOptions();
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // IIdempotentCommand を実装していない場合はスキップ
        if (request is not IIdempotentCommand idempotentCommand)
        {
            return await next();
        }

        // ストアが登録されていない場合はスキップ
        if (_idempotencyStore is null)
        {
            _logger.LogDebug(
                "IdempotencyStore が登録されていないため冪等性チェックをスキップします: {RequestType}",
                typeof(TRequest).Name);
            return await next();
        }

        var key = idempotentCommand.IdempotencyKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning(
                "IdempotencyKeyが空のため冪等性チェックをスキップします: {RequestType}",
                typeof(TRequest).Name);
            return await next();
        }

        // 既存レコードをチェック
        var existingRecord = await _idempotencyStore.GetAsync(key, cancellationToken);
        if (existingRecord is not null)
        {
            return HandleExistingRecord(existingRecord, key);
        }

        // 処理中レコードを作成
        var processingRecord = new IdempotencyRecord
        {
            Key = key,
            RequestType = typeof(TRequest).FullName ?? typeof(TRequest).Name,
            ResponseJson = string.Empty,
            ResponseType = typeof(TResponse).FullName ?? typeof(TResponse).Name,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(_options.DefaultExpiration),
            Status = IdempotencyStatus.Processing
        };

        await _idempotencyStore.SaveAsync(processingRecord, cancellationToken);

        try
        {
            var response = await next();

            // 成功レコードを保存
            var completedRecord = processingRecord with
            {
                ResponseJson = SerializeResponse(response),
                Status = IdempotencyStatus.Completed
            };

            await _idempotencyStore.SaveAsync(completedRecord, cancellationToken);

            _logger.LogDebug(
                "冪等性レコードを保存しました: Key={Key}, RequestType={RequestType}",
                key,
                typeof(TRequest).Name);

            return response;
        }
        catch (Exception ex)
        {
            // 失敗時はレコードを削除（リトライ可能にする）
            _logger.LogWarning(
                ex,
                "リクエスト処理に失敗したため冪等性レコードを削除: Key={Key}",
                key);

            await _idempotencyStore.RemoveAsync(key, cancellationToken);
            throw;
        }
    }

    private TResponse HandleExistingRecord(IdempotencyRecord record, string key)
    {
        switch (record.Status)
        {
            case IdempotencyStatus.Processing:
                _logger.LogWarning(
                    "リクエストは処理中です: Key={Key}",
                    key);
                return CreateConflictResponse("リクエストは現在処理中です。しばらくしてから再試行してください。");

            case IdempotencyStatus.Completed:
                _logger.LogInformation(
                    "冪等性レコードから保存済みレスポンスを返却: Key={Key}",
                    key);
                return DeserializeResponse(record.ResponseJson);

            case IdempotencyStatus.Failed:
                _logger.LogWarning(
                    "以前のリクエストは失敗しています。再実行します: Key={Key}",
                    key);
                // 失敗レコードは削除してリトライを許可（このケースは通常SaveAsyncで上書きされる）
                return CreateConflictResponse("以前のリクエストは失敗しました。新しいIdempotencyKeyで再試行してください。");

            default:
                throw new InvalidOperationException($"Unknown IdempotencyStatus: {record.Status}");
        }
    }

    private static string SerializeResponse(TResponse response)
    {
        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private TResponse DeserializeResponse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<TResponse>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "冪等性レコードのデシリアライズに失敗しました");
            throw new InvalidOperationException("保存されたレスポンスのデシリアライズに失敗しました", ex);
        }
    }

    private static TResponse CreateConflictResponse(string message)
    {
        // TResponse が Result<T> または Result の場合
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Fail(message);
        }

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var genericArg = typeof(TResponse).GetGenericArguments()[0];
            // Get the generic Fail<T> method
            var failMethod = typeof(Result)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(m => m.Name == nameof(Result.Fail) && m.IsGenericMethod)
                .FirstOrDefault()
                ?.MakeGenericMethod(genericArg);

            if (failMethod is not null)
            {
                return (TResponse)failMethod.Invoke(null, [message])!;
            }
        }

        // Result系以外の場合は例外をスロー
        throw new InvalidOperationException(message);
    }
}

/// <summary>
/// 冪等性オプション
/// </summary>
public sealed class IdempotencyOptions
{
    /// <summary>
    /// デフォルトの有効期限（デフォルト: 24時間）
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(24);
}
