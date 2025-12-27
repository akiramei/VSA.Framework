namespace VSA.Application;

/// <summary>
/// 処理結果を表すクラス
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Fail(string error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Fail<T>(string error) => new(default, false, error);
}

/// <summary>
/// 値を持つ処理結果
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, string? error) : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>
    /// 暗黙的にTに変換（成功時のみ）
    /// </summary>
    public static implicit operator T?(Result<T> result) => result.Value;
}
