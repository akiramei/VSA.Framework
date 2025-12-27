# VSA.Blazor - Blazor基盤スキル

VSA.Blazorを使用したBlazorページ実装のガイドです。
UIに影響を与えないロジック層のみを提供し、デザインは完全に自由です。

## DI登録

```csharp
// Program.cs
services.AddVsaBlazor();

// これにより以下が登録されます:
// - IMediatorService: MediatR + Result統合サービス
```

## IMediatorService

MediatRとResult型を統合したサービスです。エラーハンドリングを簡素化します。

```csharp
@inject IMediatorService Mediator
@inject ISnackbar Snackbar

@code {
    private IReadOnlyList<BookDto>? _books;

    protected override async Task OnInitializedAsync()
    {
        // クエリ実行（エラー時はコールバック）
        _books = await Mediator.QueryAsync(
            new GetBooksQuery(null),
            onError: msg => Snackbar.Add(msg, Severity.Error));
    }

    private async Task CreateBook()
    {
        // コマンド実行
        var bookId = await Mediator.CommandAsync(
            new CreateBookCommand("タイトル", "著者"),
            onError: msg => Snackbar.Add(msg, Severity.Error));

        if (bookId != Guid.Empty)
        {
            Snackbar.Add("図書を作成しました", Severity.Success);
        }
    }

    private async Task DeleteBook(Guid bookId)
    {
        // 結果なしコマンド
        var success = await Mediator.CommandAsync(
            new DeleteBookCommand(bookId),
            onError: msg => Snackbar.Add(msg, Severity.Error));

        if (success)
        {
            Snackbar.Add("削除しました", Severity.Success);
            await LoadBooks();
        }
    }
}
```

## ListPageBase（リストページ基底クラス）

リスト表示ページのロジックを提供します。UIは完全に自由です。

```csharp
// ページ実装
@page "/books"
@inherits ListPageBase<GetBooksQuery, BookDto>
@inject ISnackbar Snackbar

<PageTitle>図書一覧</PageTitle>

<!-- UI は完全に自由 -->
<MudText Typo="Typo.h4">図書一覧</MudText>

<MudTextField @bind-Value="_searchTerm" Label="検索"
              OnKeyUp="OnSearchKeyUp" />

@if (Loading)
{
    <MudProgressLinear Indeterminate="true" />
}
else if (IsEmpty)
{
    <MudAlert Severity="Severity.Info">データがありません</MudAlert>
}
else
{
    <MudTable Items="@Items">
        <HeaderContent>
            <MudTh>タイトル</MudTh>
            <MudTh>著者</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd>@context.Title</MudTd>
            <MudTd>@context.Author</MudTd>
        </RowTemplate>
    </MudTable>
}

@code {
    private string _searchTerm = "";

    // クエリ生成をオーバーライド
    protected override GetBooksQuery CreateQuery()
        => new(_searchTerm);

    // エラー処理をオーバーライド
    protected override void OnError(string errorMessage)
        => Snackbar.Add(errorMessage, Severity.Error);

    private async Task OnSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await LoadAsync();
    }
}
```

### ListPageBaseが提供するプロパティ/メソッド

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| Items | IReadOnlyList<TDto>? | 取得したアイテム |
| Loading | bool | ローディング中かどうか |
| Error | string? | エラーメッセージ |
| HasItems | bool | アイテムが存在するか |
| IsEmpty | bool | アイテムが空か |

| メソッド | 説明 |
|---------|------|
| CreateQuery() | オーバーライドしてクエリを生成 |
| OnError(string) | オーバーライドしてエラー処理 |
| LoadAsync() | データを読み込み |
| ReloadAsync() | データを再読み込み |

## PagedListPageBase（ページング付きリスト基底クラス）

ページング付きリスト表示のロジックを提供します。

```csharp
@page "/members"
@inherits PagedListPageBase<GetMembersPagedQuery, MemberDto>
@inject ISnackbar Snackbar

<MudTable Items="@Items" Dense="true">
    <!-- テーブル内容 -->
</MudTable>

<MudPagination Count="@TotalPages"
               Selected="@CurrentPage"
               SelectedChanged="OnPageChanged" />

@code {
    protected override GetMembersPagedQuery CreateQuery(int page, int pageSize)
        => new(page, pageSize, _searchTerm);

    protected override void OnError(string errorMessage)
        => Snackbar.Add(errorMessage, Severity.Error);

    private async Task OnPageChanged(int page)
        => await GoToPageAsync(page);
}
```

### PagedListPageBaseが提供するプロパティ/メソッド

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| PagedResult | PagedResult<TDto>? | ページング結果 |
| Items | IReadOnlyList<TDto> | 現在ページのアイテム |
| CurrentPage | int | 現在のページ番号（1始まり） |
| PageSize | int | ページサイズ |
| TotalPages | int | 総ページ数 |
| TotalCount | int | 総アイテム数 |
| HasPreviousPage | bool | 前ページの有無 |
| HasNextPage | bool | 次ページの有無 |

| メソッド | 説明 |
|---------|------|
| CreateQuery(page, pageSize) | オーバーライドしてクエリを生成 |
| GoToPageAsync(page) | 指定ページに移動 |
| NextPageAsync() | 次ページに移動 |
| PreviousPageAsync() | 前ページに移動 |

## FormPageBase（フォームページ基底クラス）

フォームページのロジックを提供します。UIは完全に自由です。

```csharp
@page "/books/new"
@inherits FormPageBase<CreateBookCommand, Guid>
@inject ISnackbar Snackbar

<PageTitle>図書登録</PageTitle>

<MudForm @ref="_form" @bind-IsValid="FormValid">
    <MudTextField @bind-Value="_title" Label="タイトル" Required="true" />
    <MudTextField @bind-Value="_author" Label="著者" Required="true" />

    <MudButton Disabled="@SubmitDisabled" OnClick="SubmitAsync">
        @if (Submitting)
        {
            <MudProgressCircular Size="Size.Small" Indeterminate="true" />
        }
        登録
    </MudButton>
</MudForm>

@code {
    private MudForm? _form;
    private string _title = "";
    private string _author = "";

    // コマンド生成をオーバーライド
    protected override CreateBookCommand CreateCommand()
        => new(_title, _author);

    // 成功メッセージ
    protected override string SuccessMessage => "図書を登録しました";

    // 成功時のナビゲーション先
    protected override string SuccessNavigateTo => "/books";

    // 成功時の処理をオーバーライド
    protected override void OnSuccess(string message, Guid result)
        => Snackbar.Add(message, Severity.Success);

    // エラー処理をオーバーライド
    protected override void OnError(string errorMessage)
        => Snackbar.Add(errorMessage, Severity.Error);
}
```

### FormPageBaseが提供するプロパティ/メソッド

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| FormValid | bool | フォームが有効か（バインド用） |
| Submitting | bool | 送信中かどうか |
| Error | string? | エラーメッセージ |
| SubmitDisabled | bool | 送信ボタンを無効にするか |

| メソッド/プロパティ | 説明 |
|---------|------|
| CreateCommand() | オーバーライドしてコマンドを生成 |
| SuccessMessage | オーバーライドして成功メッセージを指定 |
| SuccessNavigateTo | オーバーライドして成功時の遷移先を指定 |
| OnSuccess(message, result) | オーバーライドして成功時処理 |
| OnError(errorMessage) | オーバーライドしてエラー処理 |
| SubmitAsync() | フォームを送信 |

## ObservableState（状態管理）

リアクティブな状態管理を提供します。

```csharp
@code {
    private ObservableState<BookDto> _book = new();

    protected override async Task OnInitializedAsync()
    {
        _book.OnStateChanged = StateHasChanged;

        await _book.LoadAsync(async () =>
        {
            return await Mediator.QueryAsync(new GetBookByIdQuery(BookId));
        });
    }
}

<!-- 使用例 -->
@if (_book.IsLoading)
{
    <MudProgressCircular Indeterminate="true" />
}
else if (_book.HasError)
{
    <MudAlert Severity="Severity.Error">@_book.Error</MudAlert>
}
else if (_book.HasValue)
{
    <MudText>@_book.Value.Title</MudText>
}
```

## LoadingState（ローディング状態）

複数の非同期操作のローディング状態を追跡します。

```csharp
@code {
    private LoadingState _loading = new();

    protected override void OnInitialized()
    {
        _loading.OnStateChanged = StateHasChanged;
    }

    private async Task LoadData()
    {
        await _loading.ExecuteAsync(async () =>
        {
            _books = await Mediator.QueryAsync(new GetBooksQuery(null));
        });
    }

    private async Task LoadMultiple()
    {
        // 複数の操作を追跡
        _loading.StartLoading();
        try
        {
            var task1 = LoadBooks();
            var task2 = LoadCategories();
            await Task.WhenAll(task1, task2);
        }
        finally
        {
            _loading.StopLoading();
        }
    }
}

@if (_loading.IsLoading)
{
    <MudProgressLinear Indeterminate="true" />
}
```

## ディレクトリ構成例

```
src/
└── Web/
    └── Components/
        └── Pages/
            └── Books/
                ├── BookList.razor      # @inherits ListPageBase
                ├── BookCreate.razor    # @inherits FormPageBase
                ├── BookEdit.razor      # @inherits FormPageBase
                └── BookDetail.razor    # IMediatorService直接使用
```

## 重要: UIの自由度

VSA.Blazorはロジック層のみを提供します。以下は**完全に自由**です:

- UIフレームワーク（MudBlazor, Radzen, Bootstrap, 独自CSS）
- レイアウト構造
- コンポーネント配置
- スタイリング
- アニメーション

基底クラスはデータと操作のみを提供し、見た目は一切強制しません。
