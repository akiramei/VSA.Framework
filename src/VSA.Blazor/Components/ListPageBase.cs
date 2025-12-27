using Microsoft.AspNetCore.Components;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Blazor.Services;

namespace VSA.Blazor.Components;

/// <summary>
/// リスト表示ページの基底クラス。
/// UIテンプレートは含まず、ロジックのみを提供します。
/// デザイナーはUIを完全に自由に実装できます。
/// </summary>
/// <typeparam name="TQuery">クエリの型（IQuery&lt;Result&lt;IReadOnlyList&lt;TDto&gt;&gt;&gt;を実装）</typeparam>
/// <typeparam name="TDto">DTOの型</typeparam>
public abstract class ListPageBase<TQuery, TDto> : ComponentBase
    where TQuery : IQuery<Result<IReadOnlyList<TDto>>>
{
    /// <summary>
    /// MediatRサービス。
    /// </summary>
    [Inject]
    protected IMediatorService Mediator { get; set; } = default!;

    /// <summary>
    /// 取得したアイテムリスト。
    /// </summary>
    protected IReadOnlyList<TDto>? Items { get; private set; }

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
    protected bool HasItems => Items is { Count: > 0 };

    /// <summary>
    /// アイテムが空かどうか。
    /// </summary>
    protected bool IsEmpty => !Loading && (Items is null || Items.Count == 0);

    /// <summary>
    /// クエリを作成します。
    /// 派生クラスでオーバーライドして検索条件などを設定します。
    /// </summary>
    /// <returns>実行するクエリ</returns>
    protected abstract TQuery CreateQuery();

    /// <summary>
    /// エラー発生時に呼び出されます。
    /// 派生クラスでオーバーライドしてSnackbar表示などを行います。
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
            var query = CreateQuery();
            Items = await Mediator.QueryAsync(query, OnError);
        }
        finally
        {
            Loading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// データをリロードします（LoadAsyncと同じ）。
    /// </summary>
    protected Task ReloadAsync() => LoadAsync();

    /// <summary>
    /// コンポーネント初期化時にデータをロードします。
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }
}
