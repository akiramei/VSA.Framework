using Microsoft.AspNetCore.Components;
using VSA.Application;
using VSA.Application.Common;
using VSA.Application.Interfaces;
using VSA.Blazor.Services;

namespace VSA.Blazor.Components;

/// <summary>
/// ページング付きリスト表示ページの基底クラス。
/// UIテンプレートは含まず、ロジックのみを提供します。
/// </summary>
/// <typeparam name="TQuery">クエリの型（IQuery&lt;Result&lt;PagedResult&lt;TDto&gt;&gt;&gt;を実装）</typeparam>
/// <typeparam name="TDto">DTOの型</typeparam>
public abstract class PagedListPageBase<TQuery, TDto> : ComponentBase
    where TQuery : IQuery<Result<PagedResult<TDto>>>
{
    /// <summary>
    /// MediatRサービス。
    /// </summary>
    [Inject]
    protected IMediatorService Mediator { get; set; } = default!;

    /// <summary>
    /// ページング結果。
    /// </summary>
    protected PagedResult<TDto>? PagedResult { get; private set; }

    /// <summary>
    /// 取得したアイテムリスト。
    /// </summary>
    protected IReadOnlyList<TDto> Items => PagedResult?.Items ?? [];

    /// <summary>
    /// 現在のページ番号（1始まり）。
    /// </summary>
    protected int CurrentPage { get; set; } = 1;

    /// <summary>
    /// ページサイズ。
    /// </summary>
    protected int PageSize { get; set; } = 10;

    /// <summary>
    /// 総ページ数。
    /// </summary>
    protected int TotalPages => PagedResult?.TotalPages ?? 0;

    /// <summary>
    /// 総アイテム数。
    /// </summary>
    protected int TotalCount => PagedResult?.TotalCount ?? 0;

    /// <summary>
    /// ローディング中かどうか。
    /// </summary>
    protected bool Loading { get; private set; } = true;

    /// <summary>
    /// エラーメッセージ（エラーがない場合はnull）。
    /// </summary>
    protected string? Error { get; private set; }

    /// <summary>
    /// アイテムが存在するかどうか。
    /// </summary>
    protected bool HasItems => Items.Count > 0;

    /// <summary>
    /// アイテムが空かどうか。
    /// </summary>
    protected bool IsEmpty => !Loading && Items.Count == 0;

    /// <summary>
    /// 前のページが存在するかどうか。
    /// </summary>
    protected bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// 次のページが存在するかどうか。
    /// </summary>
    protected bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// クエリを作成します。
    /// 派生クラスでオーバーライドして検索条件などを設定します。
    /// </summary>
    /// <param name="page">ページ番号</param>
    /// <param name="pageSize">ページサイズ</param>
    /// <returns>実行するクエリ</returns>
    protected abstract TQuery CreateQuery(int page, int pageSize);

    /// <summary>
    /// エラー発生時に呼び出されます。
    /// </summary>
    /// <param name="errorMessage">エラーメッセージ</param>
    protected virtual void OnError(string errorMessage)
    {
        Error = errorMessage;
    }

    /// <summary>
    /// データをロードします。
    /// </summary>
    protected async Task LoadAsync()
    {
        Loading = true;
        Error = null;
        StateHasChanged();

        try
        {
            var query = CreateQuery(CurrentPage, PageSize);
            PagedResult = await Mediator.QueryPagedAsync(query, OnError);
        }
        finally
        {
            Loading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// 指定したページに移動します。
    /// </summary>
    /// <param name="page">ページ番号</param>
    protected async Task GoToPageAsync(int page)
    {
        if (page < 1 || page > TotalPages) return;
        CurrentPage = page;
        await LoadAsync();
    }

    /// <summary>
    /// 次のページに移動します。
    /// </summary>
    protected Task NextPageAsync() => GoToPageAsync(CurrentPage + 1);

    /// <summary>
    /// 前のページに移動します。
    /// </summary>
    protected Task PreviousPageAsync() => GoToPageAsync(CurrentPage - 1);

    /// <summary>
    /// 最初のページに移動します。
    /// </summary>
    protected Task FirstPageAsync() => GoToPageAsync(1);

    /// <summary>
    /// 最後のページに移動します。
    /// </summary>
    protected Task LastPageAsync() => GoToPageAsync(TotalPages);

    /// <summary>
    /// コンポーネント初期化時にデータをロードします。
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }
}
