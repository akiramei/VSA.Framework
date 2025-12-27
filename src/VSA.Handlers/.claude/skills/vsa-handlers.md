# VSA.Handlers - 汎用ハンドラースキル

VSA.Handlersを使用した汎用CRUD操作のガイドです。
シンプルなCRUD操作はHandlerを個別実装せず、汎用ハンドラーで処理できます。

## DI登録

```csharp
// Program.cs
services.AddVsaHandlers();
```

## 汎用Repositoryインターフェース

汎用ハンドラーはIRepositoryを使用します。

```csharp
// VSA.Handlersが提供するインターフェース
public interface IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : struct
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(TEntity entity, CancellationToken ct = default);
}

// Entity Frameworkでの実装例
public class EfRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : struct
{
    private readonly AppDbContext _context;

    public EfRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default)
    {
        return await _context.Set<TEntity>()
            .FirstOrDefaultAsync(e => e.Id.Equals(id), ct);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Set<TEntity>().ToListAsync(ct);
    }

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await _context.Set<TEntity>().AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        _context.Set<TEntity>().Update(entity);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        _context.Set<TEntity>().Remove(entity);
        await _context.SaveChangesAsync(ct);
    }
}

// DI登録
services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
```

## CreateEntityHandler（作成）

エンティティの作成を汎用的に処理します。

```csharp
// コマンド定義
public record CreateEntityCommand<TEntity, TId>(TEntity Entity)
    : ICommand<Result<TId>>
    where TEntity : Entity<TId>
    where TId : struct;

// 使用例
var command = new CreateEntityCommand<Book, BookId>(book);
var result = await _mediator.Send(command);

// カスタムCreateCommandを使う場合（推奨）
public record CreateBookCommand(string Title, string Author)
    : ICommand<Result<Guid>>;

// ハンドラーで汎用リポジトリを使用
public class CreateBookHandler : IRequestHandler<CreateBookCommand, Result<Guid>>
{
    private readonly IRepository<Book, BookId> _repository;

    public async Task<Result<Guid>> Handle(CreateBookCommand request, CancellationToken ct)
    {
        var book = Book.Create(request.Title, request.Author);
        await _repository.AddAsync(book, ct);
        return Result.Success(book.Id.Value);
    }
}
```

## GetByIdQueryHandler（単一取得）

IDによるエンティティ取得を汎用的に処理します。

```csharp
// クエリ定義
public record GetEntityByIdQuery<TEntity, TId, TDto>(TId Id)
    : IQuery<Result<TDto>>
    where TEntity : Entity<TId>
    where TId : struct;

// 使用例
var query = new GetEntityByIdQuery<Book, BookId, BookDto>(bookId);
var result = await _mediator.Send(query);

// カスタムQueryを使う場合（推奨）
public record GetBookByIdQuery(Guid BookId) : IQuery<Result<BookDto>>;

public class GetBookByIdHandler : IRequestHandler<GetBookByIdQuery, Result<BookDto>>
{
    private readonly IRepository<Book, BookId> _repository;

    public async Task<Result<BookDto>> Handle(GetBookByIdQuery request, CancellationToken ct)
    {
        var book = await _repository.GetByIdAsync(new BookId(request.BookId), ct);

        if (book is null)
            return Result.Fail<BookDto>("図書が見つかりません");

        return Result.Success(new BookDto(book.Id.Value, book.Title, book.Author));
    }
}
```

## GetListQueryHandler（一覧取得）

エンティティ一覧取得を汎用的に処理します。

```csharp
// クエリ定義
public record GetEntityListQuery<TEntity, TId, TDto>
    : IQuery<Result<IReadOnlyList<TDto>>>
    where TEntity : Entity<TId>
    where TId : struct;

// カスタムQueryを使う場合（推奨）
public record GetBooksQuery(string? SearchTerm) : IQuery<Result<IReadOnlyList<BookDto>>>;

public class GetBooksHandler : IRequestHandler<GetBooksQuery, Result<IReadOnlyList<BookDto>>>
{
    private readonly IRepository<Book, BookId> _repository;

    public async Task<Result<IReadOnlyList<BookDto>>> Handle(GetBooksQuery request, CancellationToken ct)
    {
        var books = await _repository.GetAllAsync(ct);

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            books = books.Where(b =>
                b.Title.Contains(request.SearchTerm) ||
                b.Author.Contains(request.SearchTerm)
            ).ToList();
        }

        var dtos = books.Select(b => new BookDto(b.Id.Value, b.Title, b.Author)).ToList();
        return Result.Success<IReadOnlyList<BookDto>>(dtos);
    }
}
```

## UpdateEntityHandler（更新）

エンティティの更新を汎用的に処理します。

```csharp
// コマンド定義
public record UpdateEntityCommand<TEntity, TId>(TId Id, Action<TEntity> UpdateAction)
    : ICommand<Result>
    where TEntity : Entity<TId>
    where TId : struct;

// カスタムCommandを使う場合（推奨）
public record UpdateBookCommand(Guid BookId, string Title, string Author)
    : ICommand<Result<BookDto>>;

public class UpdateBookHandler : IRequestHandler<UpdateBookCommand, Result<BookDto>>
{
    private readonly IRepository<Book, BookId> _repository;

    public async Task<Result<BookDto>> Handle(UpdateBookCommand request, CancellationToken ct)
    {
        var book = await _repository.GetByIdAsync(new BookId(request.BookId), ct);

        if (book is null)
            return Result.Fail<BookDto>("図書が見つかりません");

        book.Update(request.Title, request.Author);
        await _repository.UpdateAsync(book, ct);

        return Result.Success(new BookDto(book.Id.Value, book.Title, book.Author));
    }
}
```

## DeleteEntityHandler（削除）

エンティティの削除を汎用的に処理します。

```csharp
// コマンド定義
public record DeleteEntityCommand<TEntity, TId>(TId Id)
    : ICommand<Result>
    where TEntity : Entity<TId>
    where TId : struct;

// カスタムCommandを使う場合（推奨）
public record DeleteBookCommand(Guid BookId) : ICommand<Result>;

public class DeleteBookHandler : IRequestHandler<DeleteBookCommand, Result>
{
    private readonly IRepository<Book, BookId> _repository;

    public async Task<Result> Handle(DeleteBookCommand request, CancellationToken ct)
    {
        var book = await _repository.GetByIdAsync(new BookId(request.BookId), ct);

        if (book is null)
            return Result.Fail("図書が見つかりません");

        // ドメインルールチェック
        var canDelete = book.CanDelete();
        if (!canDelete)
            return Result.Fail(canDelete.Reason!);

        await _repository.DeleteAsync(book, ct);
        return Result.Success();
    }
}
```

## 汎用ハンドラーを使う場合 vs カスタムハンドラー

| シナリオ | 推奨 |
|---------|------|
| 単純なCRUD（ロジックなし） | 汎用ハンドラー |
| ドメインルールあり | カスタムハンドラー |
| 複雑な検索条件 | カスタムハンドラー |
| 関連エンティティの操作 | カスタムハンドラー |
| DTOへの変換が必要 | カスタムハンドラー |

## 推奨パターン

ほとんどの場合、**カスタムCommand/Query + IRepositoryを使用した実装**が推奨されます。

```csharp
// 1. Command/Query定義
public record CreateBookCommand(string Title, string Author) : ICommand<Result<Guid>>;

// 2. Handler実装（IRepositoryを使用）
public class CreateBookHandler : IRequestHandler<CreateBookCommand, Result<Guid>>
{
    private readonly IRepository<Book, BookId> _repository;

    public CreateBookHandler(IRepository<Book, BookId> repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateBookCommand request, CancellationToken ct)
    {
        var book = Book.Create(request.Title, request.Author);
        await _repository.AddAsync(book, ct);
        return Result.Success(book.Id.Value);
    }
}
```

これにより:
- ドメインロジックを適切に実装できる
- テストが容易（IRepositoryをモック可能）
- コードが明確で追跡しやすい
