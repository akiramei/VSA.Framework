# VSA.Framework

.NET 9向けのVertical Slice Architecture (VSA) フレームワーク。DDD + CQRS + MediatRパターンを提供し、AI開発支援のためのSkillsを含みます。

## パッケージ

| パッケージ | 説明 |
|-----------|------|
| **VSA.Kernel** | DDD基盤（Entity, AggregateRoot, ValueObject, ITypedId, BoundaryDecision） |
| **VSA.Application** | CQRS基盤（ICommand, IQuery, Result, PagedResult） |
| **VSA.Infrastructure** | Pipeline Behaviors（Validation, Logging, Transaction, Caching, Authorization, AuditLog, Idempotency） |
| **VSA.Handlers** | 汎用ハンドラー（IRepository） |
| **VSA.Blazor** | Blazor基盤（IMediatorService, ListPageBase, FormPageBase, ObservableState） |

## インストール

```bash
dotnet add package VSA.Kernel
dotnet add package VSA.Application
dotnet add package VSA.Infrastructure
dotnet add package VSA.Handlers
dotnet add package VSA.Blazor
```

## 特徴

### AI開発支援（Skills）

各パッケージには `.claude/skills/` にAI向けのガイドが含まれています。ビルド時に自動配置され、Claude CodeなどのAIツールが即座にフレームワークを理解できます。

```
プロジェクト/
└── .claude/
    └── skills/
        ├── vsa-kernel.md
        ├── vsa-application.md
        ├── vsa-infrastructure.md
        ├── vsa-handlers.md
        └── vsa-blazor.md
```

### DDD基盤（VSA.Kernel）

```csharp
// TypedId
public readonly record struct BookId(Guid Value) : ITypedId;

// Entity
public class Book : Entity<BookId>
{
    public string Title { get; private set; }

    public BoundaryDecision CanDelete()
    {
        if (HasActiveLoans)
            return BoundaryDecision.Deny("貸出中は削除できません");
        return BoundaryDecision.Allow();
    }
}
```

### CQRS基盤（VSA.Application）

```csharp
// Command
public record CreateBookCommand(string Title, string Author)
    : ICommand<Result<Guid>>;

// Query
public record GetBooksQuery(string? SearchTerm)
    : IQuery<Result<IReadOnlyList<BookDto>>>;

// Handler
public class CreateBookHandler : IRequestHandler<CreateBookCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBookCommand request, CancellationToken ct)
    {
        var book = Book.Create(request.Title, request.Author);
        await _repository.AddAsync(book, ct);
        return Result.Success(book.Id.Value);
    }
}
```

### Blazor基盤（VSA.Blazor）

```razor
@page "/books"
@inherits ListPageBase<GetBooksQuery, BookDto>

<MudTable Items="@Items">
    <!-- 自由なUI設計 -->
</MudTable>

@code {
    protected override GetBooksQuery CreateQuery() => new(_searchTerm);
}
```

## DI登録

```csharp
// Program.cs
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateBookHandler).Assembly));
services.AddVsaInfrastructure();  // Pipeline Behaviors
services.AddVsaBlazor();          // IMediatorService
```

## 依存関係

```
VSA.Kernel          ← 基盤（依存なし）
    ↑
VSA.Application     ← Kernel に依存
    ↑
├── VSA.Infrastructure  ← Application に依存
├── VSA.Handlers        ← Application, Kernel に依存
└── VSA.Blazor          ← Application に依存
```

## ライセンス

MIT License
