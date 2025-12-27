using FluentValidation;
using MediatR;
using VSA.Application;

namespace VSA.Infrastructure.Behaviors;

/// <summary>
/// バリデーションのPipeline Behavior
/// FluentValidationを使用してリクエストを検証
///
/// 【Pipeline順序: 100】
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

            // Result型にエラーを設定して返す
            // TResponseがResult<T>の場合は適切な型で失敗を作成
            return CreateFailureResult(errorMessage);
        }

        return await next();
    }

    private static TResponse CreateFailureResult(string errorMessage)
    {
        var responseType = typeof(TResponse);

        // Result<T>の場合
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var failMethod = typeof(Result).GetMethod(nameof(Result.Fail), 1, [typeof(string)])!;
            var genericFailMethod = failMethod.MakeGenericMethod(valueType);
            return (TResponse)genericFailMethod.Invoke(null, [errorMessage])!;
        }

        // 非ジェネリックResultの場合
        return (TResponse)(object)Result.Fail(errorMessage);
    }
}
