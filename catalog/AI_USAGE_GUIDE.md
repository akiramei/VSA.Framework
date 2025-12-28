# AI Usage Guide - VSA.Framework Pattern Catalog

このドキュメントは、AI（Claude Code等）がこのカタログを参照してVSA.Frameworkを使った業務アプリケーションを実装する際のガイドラインです。

---

## ランタイム要件（RUNTIME REQUIREMENTS）

> **重要**: このカタログは特定の .NET バージョンを前提としています。

| 項目 | 値 |
|------|-----|
| **ターゲットフレームワーク** | `net9.0` |
| **必要なSDKバージョン** | `9.0.100` 以上 |
| **VSA.Framework** | `1.3.0` 以上 |

### プロジェクト作成コマンド

```bash
# Blazor プロジェクト作成
dotnet new blazor --framework net9.0 -n MyApp

# VSA.Framework パッケージをインストール
dotnet add package VSA.Kernel
dotnet add package VSA.Application
dotnet add package VSA.Infrastructure
dotnet add package VSA.Handlers
dotnet add package VSA.Blazor
```

### DI登録

```csharp
// Program.cs
using VSA.Infrastructure.Extensions;

// VSA.Framework全機能を登録
services.AddVsaFramework(typeof(Program).Assembly);

// または個別に登録
services.AddVsaMediatR(typeof(Program).Assembly);
services.AddVsaValidation(typeof(Program).Assembly);
services.AddVsaBehaviors();
```

---

## 実装前に必ず読むこと（MUST READ）

> **[COMMON_MISTAKES.md](COMMON_MISTAKES.md) を最初に読んでください**

### なぜカタログ参照が必須なのか

VSA.Frameworkは MediatR + FluentValidation + Pipeline Behaviors を使用しています。
カタログを参照せずに実装すると、以下の問題が発生します：

| よくある失敗 | 原因 | 結果 |
|-------------|------|------|
| 独自CQRS実装 | VSA.Applicationの存在を知らない | 全面的な書き直し |
| DIライフタイム不一致 | Singleton/Scoped混在 | 実行時エラー |
| HandleAsyncメソッド名 | MediatRはHandle | コンパイルエラー |
| SaveChangesAsync呼び出し | TransactionBehaviorが自動実行 | 二重保存 |

### 実装前チェックリスト

```
□ VSA.Framework NuGetパッケージを参照しているか？
□ catalog/index.json を読んだか？
□ 該当パターンの YAML を読んだか？
□ COMMON_MISTAKES.md を確認したか？
```

---

## VSA.Framework NuGetパッケージ構成

```
VSA.Kernel (独立)
    ↑
VSA.Application
    ↑
    ├─ VSA.Infrastructure (Pipeline Behaviors)
    ├─ VSA.Handlers (汎用ハンドラー)
    └─ VSA.Blazor (UI基盤)
```

### パッケージ別の提供機能

| パッケージ | 主要クラス | 用途 |
|-----------|-----------|------|
| **VSA.Kernel** | `Entity`, `AggregateRoot<TId>`, `ValueObject`, `ITypedId`, `BoundaryDecision` | DDD基盤 |
| **VSA.Application** | `ICommand<T>`, `IQuery<T>`, `Result<T>`, `PagedResult<T>` | CQRS基盤 |
| **VSA.Infrastructure** | `ValidationBehavior`, `TransactionBehavior`, `CachingBehavior` 等 | 横断的関心事 |
| **VSA.Handlers** | `CreateEntityHandler<T>`, `GetByIdQueryHandler<T>`, `IRepository<T,TId>` | 汎用CRUD |
| **VSA.Blazor** | `ListPageBase<T>`, `FormPageBase<T>`, `IMediatorService` | UI基盤 |

---

## アーキテクチャ全体像

```
┌─────────────────────────────────────────────────────────────────────┐
│                           UI Layer                                   │
│  ┌────────────┐    ┌──────────────┐    ┌────────────────────┐      │
│  │ Component  │───▶│ PageActions  │───▶│      Store         │      │
│  │  (View)    │◀───│  (UI手順)    │◀───│  (状態管理+I/O)    │      │
│  └────────────┘    └──────────────┘    └─────────┬──────────┘      │
│                      VSA.Blazor                   │                  │
└──────────────────────────────────────────────────┼──────────────────┘
                                                   │ IMediator.Send()
┌──────────────────────────────────────────────────▼──────────────────┐
│                        MediatR Pipeline                              │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                    Pipeline Behaviors (VSA.Infrastructure)     │ │
│  │  ┌─────────┐   ┌─────────────┐   ┌─────────────┐   ┌────────┐ │ │
│  │  │Validation│──▶│Transaction  │──▶│  Caching    │──▶│Handler │ │ │
│  │  │Behavior │   │  Behavior   │   │  Behavior   │   │        │ │ │
│  │  │ (100)   │   │   (400)     │   │   (350)     │   │        │ │ │
│  │  └─────────┘   └─────────────┘   └─────────────┘   └────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                        VSA.Application + VSA.Handlers                │
└──────────────────────────────────────────────────┬──────────────────┘
                                                   │
┌──────────────────────────────────────────────────▼──────────────────┐
│                         Domain Layer                                 │
│  VSA.Kernel: AggregateRoot, Entity, ValueObject, BoundaryDecision   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## AI の参照フロー

### パターン選択の手順

1. **ユーザーの要求を分類する**
   - データ取得? → `query-pattern` カテゴリを検索
   - データ変更? → `command-pattern` カテゴリを検索
   - 横断的関心事? → `pipeline-behavior` カテゴリを検索
   - UI実装? → `ui-pattern` カテゴリを検索

2. **catalog/index.json で該当パターンを検索**
   - `ai_decision_matrix` でカテゴリを判定
   - `patterns` 配列から該当パターンを取得

3. **パターンYAMLファイルを読み込む**
   - `implementation.template` を取得
   - `ai_guidance.common_mistakes` を確認

4. **VSA.Framework基底クラスを確認**
   - 各パターンの `nuget_package` を確認
   - 該当する基底クラスを継承

---

## テンプレート変数

各パターンのYAMLには以下のテンプレート変数が使用されています：

| 変数 | 説明 | 例 |
|------|------|-----|
| `{Entity}` | エンティティ名（PascalCase） | `Product`, `Order` |
| `{entity}` | エンティティ名（camelCase） | `product`, `order` |
| `{BoundedContext}` | 境界コンテキスト名 | `ProductCatalog`, `TaskManagement` |
| `{EntityId}` | エンティティID型 | `ProductId`, `OrderId` |

---

## 実装例

### 1. エンティティ作成（VSA.Kernel使用）

```csharp
using VSA.Kernel;

// 型付きID
public readonly record struct TaskId(Guid Value) : ITypedId;

// 集約ルート
public sealed class TaskItem : AggregateRoot<TaskId>
{
    public string Title { get; private set; }
    public TaskStatus Status { get; private set; }

    private TaskItem() { } // EF Core用

    public static TaskItem Create(TaskId id, string title)
    {
        return new TaskItem { Id = id, Title = title, Status = TaskStatus.Todo };
    }

    // Boundaryメソッド
    public BoundaryDecision CanComplete()
    {
        return Status == TaskStatus.Done
            ? BoundaryDecision.Deny("既に完了しています")
            : BoundaryDecision.Allow();
    }
}
```

### 2. コマンド作成（VSA.Application使用）

```csharp
using VSA.Application;
using VSA.Application.Interfaces;

public sealed record CreateTaskCommand(
    string Title,
    string? Description
) : ICommand<Result<TaskId>>;
```

### 3. ハンドラー作成（VSA.Handlers使用）

```csharp
using VSA.Handlers.Commands;

public sealed class CreateTaskHandler
    : CreateEntityHandler<CreateTaskCommand, TaskItem, TaskId>
{
    public CreateTaskHandler(
        IRepository<TaskItem, TaskId> repository,
        ILogger<CreateTaskHandler> logger)
        : base(repository, logger)
    {
    }

    protected override TaskItem CreateEntity(CreateTaskCommand command)
    {
        var id = new TaskId(Guid.NewGuid());
        return TaskItem.Create(id, command.Title);
    }
}
```

### 4. Blazorページ作成（VSA.Blazor使用）

```razor
@inherits ListPageBase<GetTasksQuery, TaskDto>

@if (Loading)
{
    <p>Loading...</p>
}
else if (HasItems)
{
    @foreach (var task in Items)
    {
        <p>@task.Title</p>
    }
}
else
{
    <p>No tasks found.</p>
}

@code {
    protected override GetTasksQuery CreateQuery()
        => new GetTasksQuery();
}
```

---

## 禁止事項

### Handler内でSaveChangesAsyncを呼ばない

```csharp
// ❌ 禁止
await _dbContext.SaveChangesAsync(ct);

// ✅ 正しい: TransactionBehaviorが自動実行
return Result.Success(entity.Id);
```

### 独自のCQRS基盤を作らない

```csharp
// ❌ 禁止: 独自のCommand/Query型
public interface IMyCommand<T> { }

// ✅ 正しい: VSA.Applicationを使用
using VSA.Application.Interfaces;
public record MyCommand : ICommand<Result<Guid>> { }
```

### 例外でエラーを伝播しない

```csharp
// ❌ 禁止
throw new NotFoundException("Not found");

// ✅ 正しい: Result<T>を使用
return Result.Fail<Product>("Not found");
```

---

## 関連ドキュメント

- [README.md](README.md) - カタログ概要
- [COMMON_MISTAKES.md](COMMON_MISTAKES.md) - 頻出ミス集
- [index.json](index.json) - パターン索引
- VSA.Framework Skills: `.claude/skills/vsa-*.md`

---

**最終更新: 2025-12-28**
**カタログバージョン: v2025.12.28**
