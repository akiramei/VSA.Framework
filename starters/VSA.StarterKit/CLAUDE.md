# VSA.StarterKit - Claude Code ガイド

## プロジェクト概要

このプロジェクトは **VSA.Framework** を使用したサンプルアプリケーションです。
タスク管理（TaskManagement）の1つのBounded Contextを含みます。

## 技術スタック

| 技術 | バージョン | 用途 |
|------|----------|------|
| .NET | 9.0 | ランタイム |
| VSA.Kernel | 1.3.0 | DDD基盤（Entity, AggregateRoot, BoundaryDecision） |
| VSA.Application | 1.3.0 | CQRS基盤（ICommand, IQuery, Result<T>） |
| VSA.Infrastructure | 1.3.0 | Pipeline Behaviors |
| VSA.Handlers | 1.3.0 | 汎用ハンドラー |
| FluentValidation | 12.0.0 | 入力検証 |
| MediatR | 12.0.0 | CQRS |

## プロジェクト構成

```
src/
├── Domain/TaskManagement/      # ドメイン層
│   ├── TaskId.cs               # 型付きID
│   ├── TaskItem.cs             # 集約ルート
│   └── TaskStatus.cs           # 値オブジェクト
│
├── Application/Features/       # アプリケーション層
│   ├── CreateTask/
│   │   ├── CreateTaskCommand.cs
│   │   ├── CreateTaskHandler.cs
│   │   └── CreateTaskValidator.cs
│   └── GetTasks/
│       ├── GetTasksQuery.cs
│       ├── GetTasksHandler.cs
│       └── TaskDto.cs
│
└── Host.Web/                   # ホスト層
    └── Program.cs
```

## 実装パターン

### 1. 型付きID（VSA.Kernel.ITypedId）

```csharp
public readonly record struct TaskId(Guid Value) : ITypedId
{
    public static TaskId New() => new(Guid.NewGuid());
    public static TaskId From(Guid value) => new(value);
}
```

### 2. 集約ルート（VSA.Kernel.AggregateRoot）

```csharp
public sealed class TaskItem : AggregateRoot<TaskId>
{
    public BoundaryDecision CanComplete()
    {
        return Status == TaskStatus.Done
            ? BoundaryDecision.Deny("既に完了しています")
            : BoundaryDecision.Allow();
    }
}
```

### 3. コマンド（VSA.Application.ICommand）

```csharp
public sealed record CreateTaskCommand(
    string Title,
    string? Description
) : ICommand<Result<TaskId>>;
```

### 4. ハンドラー（VSA.Handlers.CreateEntityHandler）

```csharp
public sealed class CreateTaskHandler
    : CreateEntityHandler<CreateTaskCommand, TaskItem, TaskId>
{
    protected override TaskItem CreateEntity(CreateTaskCommand command)
    {
        return TaskItem.Create(TaskId.New(), command.Title, command.Description);
    }
}
```

## 重要な禁止事項

### Handler内でSaveChangesAsyncを呼ばない

```csharp
// ❌ 禁止
await _dbContext.SaveChangesAsync(ct);

// ✅ 正しい: TransactionBehaviorが自動実行
return Result.Success(entity.Id);
```

### 例外でエラーを伝播しない

```csharp
// ❌ 禁止
throw new NotFoundException("Not found");

// ✅ 正しい: Result<T>を使用
return Result.Fail<TaskId>("タスクが見つかりません");
```

### UIにビジネスロジックを書かない

```csharp
// ❌ 禁止: UIが業務ルールを知っている
@if (task.Status == TaskStatus.Done)
{
    <button disabled>完了不可</button>
}

// ✅ 正しい: Entity.CanXxx()の結果を使用
<button disabled="@(!_canComplete.IsAllowed)">完了</button>
```

## カタログ参照

機能追加時は必ず `../catalog/` を参照:

1. `catalog/index.json` でパターンを検索
2. 該当パターンのYAMLを読む
3. `ai_guidance.common_mistakes` を確認
4. `catalog/COMMON_MISTAKES.md` を確認

## コマンド

```bash
# ビルド
dotnet build

# テスト
dotnet test

# 実行
dotnet run --project src/Host.Web
```

---

**VSA.Framework v1.3.0**
