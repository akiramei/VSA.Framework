# VSA.StarterKit

VSA.Frameworkを使用したサンプルアプリケーションです。
カタログ駆動AI開発の出発点として使用できます。

## 概要

このスターターキットは、タスク管理（TaskManagement）の1つのBounded Contextを含みます。

### 実装済み機能

| 機能 | パターン | ファイル |
|------|---------|---------|
| タスク作成 | feature-create-entity | CreateTask/ |
| タスク一覧取得 | feature-search-entity | GetTasks/ |

### 技術スタック

- .NET 9.0
- VSA.Framework v1.3.0
- MediatR
- FluentValidation
- Entity Framework Core

## セットアップ

### 1. NuGetパッケージのインストール

```bash
dotnet add package VSA.Kernel
dotnet add package VSA.Application
dotnet add package VSA.Infrastructure
dotnet add package VSA.Handlers
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
```

### 2. DI登録

```csharp
// Program.cs
using VSA.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// VSA.Framework 全機能を登録
builder.Services.AddVsaFramework(typeof(Program).Assembly);

// または個別に登録
// builder.Services.AddVsaMediatR(typeof(Program).Assembly);
// builder.Services.AddVsaValidation(typeof(Program).Assembly);
// builder.Services.AddVsaBehaviors();
```

### 3. ビルド・実行

```bash
dotnet build
dotnet run --project src/Host.Web
```

## プロジェクト構造

```
src/
├── Domain/TaskManagement/      # ドメイン層
│   ├── TaskId.cs               # 型付きID (ITypedId)
│   ├── TaskItem.cs             # 集約ルート (AggregateRoot<T>)
│   └── TaskStatus.cs           # ステータスenum
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

## 機能追加の手順

### 1. カタログを参照

```
catalog/index.json         → パターン検索
catalog/patterns/*.yaml    → パターン詳細
catalog/features/*.yaml    → フィーチャースライス
catalog/COMMON_MISTAKES.md → 頻出ミス確認
```

### 2. ドメイン層から実装

1. 型付きID (`ITypedId`)
2. エンティティ (`AggregateRoot<TId>`)
3. Boundaryメソッド (`CanXxx() -> BoundaryDecision`)

### 3. アプリケーション層を実装

1. Command/Query (`ICommand<T>` / `IQuery<T>`)
2. Handler (VSA.Handlersの汎用ハンドラーを継承)
3. Validator (`AbstractValidator<T>`)

### 4. 禁止事項を確認

- Handler内でSaveChangesAsyncを呼ばない
- Validator内でDBアクセスしない
- 例外でエラーを伝播しない（Result<T>を使用）

## カタログ駆動開発

このスターターキットは、`../catalog/`ディレクトリのパターンカタログと連携します。

### 推奨ワークフロー

1. `catalog/AI_USAGE_GUIDE.md` を読む
2. `catalog/index.json` で適切なパターンを検索
3. パターンYAMLの `ai_guidance.common_mistakes` を確認
4. `catalog/COMMON_MISTAKES.md` で禁止事項を確認
5. パターンに従って実装

### AIエージェントへの指示例

```
このプロジェクトには catalog/ ディレクトリにパターンカタログがあります。
VSA.Framework NuGetパッケージを使用しています。

新機能を実装する際は:
1. catalog/index.json を読み込み、適切なパターンを検索
2. 該当パターンの YAML ファイルを読み込み
3. VSA.Framework の基底クラスを継承
4. ai_guidance の common_mistakes を確認
```

## 関連ドキュメント

- [CLAUDE.md](CLAUDE.md) - Claude Code向けガイド
- [../catalog/README.md](../catalog/README.md) - パターンカタログ
- [../catalog/COMMON_MISTAKES.md](../catalog/COMMON_MISTAKES.md) - 頻出ミス

---

**VSA.Framework v1.3.0 | .NET 9.0**
