# VSA.Application - CQRS基盤スキル

VSA.Applicationを使用したCommand/Query/Handlerの作成ガイドです。

## Command（コマンド）

データを変更する操作を定義します。`ICommand<Result<T>>`を実装します。

```csharp
// 結果を返すCommand（作成、更新で新しいIDや値を返す場合）
public record CreateBookCommand(
    string Title,
    string Author,
    string? Isbn
) : ICommand<Result<Guid>>;

public record UpdateBookCommand(
    Guid BookId,
    string Title,
    string Author
) : ICommand<Result<BookDto>>;

// 結果を返さないCommand（削除など）
public record DeleteBookCommand(Guid BookId) : ICommand<Result>;

public record DeactivateMemberCommand(Guid MemberId) : ICommand<Result>;
```

## Query（クエリ）

データを取得する操作を定義します。`IQuery<Result<T>>`を実装します。

```csharp
// 単一取得
public record GetBookByIdQuery(Guid BookId) : IQuery<Result<BookDto>>;

// リスト取得
public record GetBooksQuery(
    string? SearchTerm,
    bool IncludeInactive = false
) : IQuery<Result<IReadOnlyList<BookDto>>>;

// ページング取得
public record GetBooksPagedQuery(
    int Page,
    int PageSize,
    string? SearchTerm
) : IQuery<Result<PagedResult<BookDto>>>;
```

## Handler（ハンドラー）

CommandまたはQueryを処理するロジックを実装します。

```csharp
// Command Handler
public class CreateBookHandler : IRequestHandler<CreateBookCommand, Result<Guid>>
{
    private readonly IBookRepository _repository;

    public CreateBookHandler(IBookRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(
        CreateBookCommand request,
        CancellationToken cancellationToken)
    {
        // ドメインモデルを作成
        var book = Book.Create(request.Title, request.Author);

        // 永続化
        await _repository.AddAsync(book, cancellationToken);

        // 成功結果を返す
        return Result.Success(book.Id.Value);
    }
}

// Query Handler
public class GetBooksHandler
    : IRequestHandler<GetBooksQuery, Result<IReadOnlyList<BookDto>>>
{
    private readonly IBookRepository _repository;

    public GetBooksHandler(IBookRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<BookDto>>> Handle(
        GetBooksQuery request,
        CancellationToken cancellationToken)
    {
        var books = await _repository.GetAllAsync(
            request.SearchTerm,
            request.IncludeInactive,
            cancellationToken);

        var dtos = books.Select(b => new BookDto(
            b.Id.Value,
            b.Title,
            b.Author,
            b.IsActive
        )).ToList();

        return Result.Success<IReadOnlyList<BookDto>>(dtos);
    }
}
```

## Result（結果型）

操作の成功/失敗を表現します。

```csharp
// 成功
return Result.Success();                    // 値なし
return Result.Success(book.Id.Value);       // 値あり
return Result.Success<IReadOnlyList<T>>(items);

// 失敗
return Result.Fail("エラーメッセージ");      // 値なし
return Result.Fail<Guid>("作成に失敗しました"); // 値あり

// Handler内での使用例
public async Task<Result<Guid>> Handle(UpdateBookCommand request, CancellationToken ct)
{
    var book = await _repository.GetByIdAsync(new BookId(request.BookId), ct);

    if (book is null)
        return Result.Fail<Guid>("図書が見つかりません");

    var canUpdate = book.CanUpdate();
    if (!canUpdate)
        return Result.Fail<Guid>(canUpdate.Reason!);

    book.Update(request.Title, request.Author);
    await _repository.UpdateAsync(book, ct);

    return Result.Success(book.Id.Value);
}
```

## PagedResult（ページング結果）

ページング情報を含む結果を表現します。

```csharp
// Query定義
public record GetMembersPagedQuery(
    int Page,
    int PageSize,
    string? SearchTerm
) : IQuery<Result<PagedResult<MemberDto>>>;

// Handler実装
public async Task<Result<PagedResult<MemberDto>>> Handle(
    GetMembersPagedQuery request,
    CancellationToken ct)
{
    var (items, totalCount) = await _repository.GetPagedAsync(
        request.Page,
        request.PageSize,
        request.SearchTerm,
        ct);

    var dtos = items.Select(m => new MemberDto(...)).ToList();

    var result = PagedResult<MemberDto>.Create(
        items: dtos,
        totalCount: totalCount,
        currentPage: request.Page,
        pageSize: request.PageSize
    );

    return Result.Success(result);
}

// PagedResultのプロパティ
// - Items: データリスト
// - TotalCount: 全件数
// - CurrentPage: 現在のページ（1始まり）
// - PageSize: ページサイズ
// - TotalPages: 総ページ数（計算済み）
// - HasPreviousPage: 前ページの有無
// - HasNextPage: 次ページの有無
```

## DTO（データ転送オブジェクト）

レイヤー間のデータ転送に使用します。recordで定義します。

```csharp
// 基本的なDTO
public record BookDto(
    Guid Id,
    string Title,
    string Author,
    bool IsActive
);

// 詳細DTO（関連データを含む）
public record BookDetailDto(
    Guid Id,
    string Title,
    string Author,
    bool IsActive,
    IReadOnlyList<LoanHistoryDto> LoanHistory
);

// リスト用の軽量DTO
public record BookListItemDto(
    Guid Id,
    string Title,
    string Author
);
```

## 認可属性

認可が必要なコマンドに属性を付与します。

```csharp
[Authorize(Roles = "Admin")]
public record DeleteBookCommand(Guid BookId) : ICommand<Result>;

[Authorize(Roles = "Admin,Librarian")]
public record UpdateBookCommand(...) : ICommand<Result<BookDto>>;

[Authorize(Policy = "CanManageMembers")]
public record SuspendMemberCommand(Guid MemberId) : ICommand<Result>;
```

## 冪等性コマンド

同じリクエストを複数回実行しても結果が同じになるコマンドです。

```csharp
public record ProcessPaymentCommand(
    Guid OrderId,
    decimal Amount
) : ICommand<Result<Guid>>, IIdempotentCommand
{
    // リクエストを一意に識別するキー
    public string IdempotencyKey => $"payment:{OrderId}";
}
```

## キャッシュ可能クエリ

結果をキャッシュするクエリです。

```csharp
public record GetBookCategoriesQuery : IQuery<Result<IReadOnlyList<CategoryDto>>>, ICacheableQuery
{
    public string CacheKey => "book-categories";
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);
}
```

## ディレクトリ構成例

```
src/
└── Application/
    └── Features/
        └── Books/
            ├── Commands/
            │   ├── CreateBookCommand.cs
            │   ├── CreateBookHandler.cs
            │   ├── UpdateBookCommand.cs
            │   └── UpdateBookHandler.cs
            ├── Queries/
            │   ├── GetBookByIdQuery.cs
            │   ├── GetBookByIdHandler.cs
            │   ├── GetBooksQuery.cs
            │   └── GetBooksHandler.cs
            └── Dtos/
                ├── BookDto.cs
                └── BookDetailDto.cs
```

## DI登録

```csharp
// Program.cs
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateBookHandler).Assembly));
```
