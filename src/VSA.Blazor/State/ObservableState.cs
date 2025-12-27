namespace VSA.Blazor.State;

/// <summary>
/// リアクティブな状態管理クラス。
/// 値の変更とローディング状態を追跡し、変更時にコールバックを発火します。
/// </summary>
/// <typeparam name="T">状態の型</typeparam>
public class ObservableState<T>
{
    private T? _value;
    private bool _isLoading;
    private string? _error;

    /// <summary>
    /// 状態変更時に呼び出されるコールバック。
    /// BlazorコンポーネントでStateHasChangedを呼び出すために使用します。
    /// </summary>
    public Action? OnStateChanged { get; set; }

    /// <summary>
    /// 現在の値。
    /// </summary>
    public T? Value
    {
        get => _value;
        private set
        {
            _value = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// ローディング中かどうか。
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// エラーメッセージ（エラーがない場合はnull）。
    /// </summary>
    public string? Error
    {
        get => _error;
        private set
        {
            _error = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// 値が存在するかどうか。
    /// </summary>
    public bool HasValue => _value is not null;

    /// <summary>
    /// エラーが発生しているかどうか。
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(_error);

    /// <summary>
    /// 非同期でデータをロードします。
    /// </summary>
    /// <param name="loader">データを取得する非同期関数</param>
    /// <param name="onError">エラー時のコールバック（オプション）</param>
    public async Task LoadAsync(Func<Task<T?>> loader, Action<string>? onError = null)
    {
        IsLoading = true;
        Error = null;

        try
        {
            Value = await loader();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            onError?.Invoke(ex.Message);
            Value = default;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 値を直接設定します。
    /// </summary>
    /// <param name="value">設定する値</param>
    public void SetValue(T? value)
    {
        Error = null;
        Value = value;
    }

    /// <summary>
    /// エラーを設定します。
    /// </summary>
    /// <param name="error">エラーメッセージ</param>
    public void SetError(string error)
    {
        Error = error;
    }

    /// <summary>
    /// 状態をリセットします。
    /// </summary>
    public void Reset()
    {
        _value = default;
        _isLoading = false;
        _error = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
