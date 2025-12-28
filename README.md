# VSA.Framework

.NET 9向けのVertical Slice Architecture (VSA) フレームワーク。DDD + CQRS + MediatRパターンを提供し、AI駆動開発のためのパターンカタログとSkillsを含みます。

## カタログ駆動AI開発

VSA.Frameworkは **パターンカタログ** を同梱しており、AIエージェント（Claude Code等）が一貫性のあるコードを生成できます。

```
VSA.Framework/
├── src/                    # NuGetパッケージ群
├── catalog/                # パターンカタログ（AI参照用）
│   ├── index.json          # パターン索引
│   ├── patterns/           # Pipeline Behaviors, Domain Patterns
│   ├── features/           # Feature Slices (CRUD)
│   └── AI_USAGE_GUIDE.md   # AI向けガイド
├── workpacks/              # ステートレスAI駆動開発
└── starters/               # スターターキット
    └── VSA.StarterKit/     # TaskManagement サンプル
```

### クイックスタート

```bash
# スターターキットをコピー
cp -r starters/VSA.StarterKit your-project

# NuGetパッケージをインストール
dotnet add package VSA.Kernel
dotnet add package VSA.Application
dotnet add package VSA.Infrastructure
```

### AIエージェントへの指示例

```
このプロジェクトには catalog/ ディレクトリにパターンカタログがあります。
新機能を実装する際は:
1. catalog/index.json を読み込み、適切なパターンを検索
2. 該当パターンの YAML ファイルを読み込み
3. VSA.Framework の基底クラスを継承
4. catalog/COMMON_MISTAKES.md を確認
```

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
