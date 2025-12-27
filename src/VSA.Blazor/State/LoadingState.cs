namespace VSA.Blazor.State;

/// <summary>
/// シンプルなローディング状態管理クラス。
/// 複数の非同期操作のローディング状態を追跡します。
/// </summary>
public class LoadingState
{
    private int _loadingCount;

    /// <summary>
    /// 状態変更時に呼び出されるコールバック。
    /// </summary>
    public Action? OnStateChanged { get; set; }

    /// <summary>
    /// ローディング中かどうか。
    /// </summary>
    public bool IsLoading => _loadingCount > 0;

    /// <summary>
    /// 非同期操作を実行し、ローディング状態を自動管理します。
    /// </summary>
    /// <typeparam name="T">結果の型</typeparam>
    /// <param name="operation">実行する非同期操作</param>
    /// <returns>操作の結果</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            StartLoading();
            return await operation();
        }
        finally
        {
            StopLoading();
        }
    }

    /// <summary>
    /// 非同期操作を実行し、ローディング状態を自動管理します。
    /// </summary>
    /// <param name="operation">実行する非同期操作</param>
    public async Task ExecuteAsync(Func<Task> operation)
    {
        try
        {
            StartLoading();
            await operation();
        }
        finally
        {
            StopLoading();
        }
    }

    /// <summary>
    /// ローディングを開始します。
    /// </summary>
    public void StartLoading()
    {
        Interlocked.Increment(ref _loadingCount);
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// ローディングを停止します。
    /// </summary>
    public void StopLoading()
    {
        var newCount = Interlocked.Decrement(ref _loadingCount);
        if (newCount < 0)
        {
            Interlocked.Exchange(ref _loadingCount, 0);
        }
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// ローディング状態をリセットします。
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _loadingCount, 0);
        OnStateChanged?.Invoke();
    }
}
