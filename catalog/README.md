# VSA.Framework Pattern Catalog

**v2025.12.28 - VSA.Framework統合版**

このディレクトリは、VSA.Frameworkを使った業務アプリケーション開発で再利用可能なパターンのカタログです。

AIエージェント（Claude Code等）が参照して、一貫性のあるコードを生成できるように設計されています。

---

## 実装前に必ず読むこと

> **[COMMON_MISTAKES.md](COMMON_MISTAKES.md) を最初に読んでください**
>
> 以下のようなミスを防ぐための重要な注意事項がまとまっています：
> - Handler内でSaveChangesAsync()を呼ばない（TransactionBehaviorが自動実行）
> - EF Core + Value Objectの比較は `.Value` ではなくインスタンス同士で行う
> - 操作可否判定はBoundary経由で行う（UIにビジネスロジックを書かない）

---

## VSA.Framework NuGetパッケージ

このカタログはVSA.Frameworkと統合されています。

| パッケージ | 説明 | 提供する機能 |
|-----------|------|-------------|
| **VSA.Kernel** | DDD基盤 | Entity, AggregateRoot, ValueObject, BoundaryDecision, ITypedId |
| **VSA.Application** | CQRS基盤 | ICommand, IQuery, Result<T>, PagedResult |
| **VSA.Infrastructure** | Pipeline Behaviors | Validation, Transaction, Caching, Authorization等 |
| **VSA.Handlers** | 汎用ハンドラー | CreateEntityHandler, GetByIdQueryHandler等 |
| **VSA.Blazor** | Blazor基盤 | ListPageBase, FormPageBase, IMediatorService |

### インストール

```bash
dotnet add package VSA.Kernel
dotnet add package VSA.Application
dotnet add package VSA.Infrastructure
dotnet add package VSA.Handlers
dotnet add package VSA.Blazor
```

---

## カタログ概要

### パターン統計

| カテゴリ | パターン数 | 説明 | NuGetパッケージ |
|---------|----------|------|----------------|
| **Kernel** | 3 | DDD基盤型 | VSA.Kernel |
| **Pipeline Behaviors** | 8 | 横断的関心事（自動実行） | VSA.Infrastructure |
| **Feature Slices** | 4 | 垂直スライス（完全な機能） | - |
| **Domain Patterns** | 4 | ドメインパターン | VSA.Kernel |
| **Query Patterns** | 2 | データ取得パターン | VSA.Handlers |
| **Command Patterns** | 1 | データ変更パターン | VSA.Handlers |

---

## パターン一覧

### Kernel（DDD基盤）

| ID | パターン名 | 目的 |
|---|----------|------|
| `result-pattern` | Result Pattern | 例外に頼らないエラーハンドリング |
| `value-object` | ValueObject Base | 値オブジェクトの基底クラス |
| `entity-base` | Entity Base | エンティティの基底クラス |

### Pipeline Behaviors（横断的関心事）

| ID | パターン名 | 順序 | 目的 |
|---|----------|-----|------|
| `validation-behavior` | ValidationBehavior | 100 | FluentValidation による入力検証 |
| `authorization-behavior` | AuthorizationBehavior | 200 | ロールベース認可チェック |
| `idempotency-behavior` | IdempotencyBehavior | 300 | 冪等性保証 |
| `caching-behavior` | CachingBehavior | 350 | Query結果キャッシュ |
| `transaction-behavior` | TransactionBehavior | 400 | トランザクション管理 |
| `audit-log-behavior` | AuditLogBehavior | 550 | 監査ログ |
| `logging-behavior` | LoggingBehavior | 600 | ログ出力 |

### Feature Slices（垂直スライス）

| ID | パターン名 | 目的 |
|---|----------|------|
| `feature-create-entity` | Create Entity | エンティティ作成 |
| `feature-update-entity` | Update Entity | エンティティ更新 |
| `feature-delete-entity` | Delete Entity | エンティティ削除 |
| `feature-search-entity` | Search Entity | 検索・フィルタリング・ページング |

### Domain Patterns（ドメインパターン）

| ID | パターン名 | 目的 |
|---|----------|------|
| `boundary-pattern` | Boundary Pattern | 操作可否判定をドメイン層に配置 |
| `domain-state-machine` | State Machine | 状態遷移管理 |
| `domain-typed-id` | Typed ID | 型安全なID |
| `domain-validation-service` | Validation Service | 複数エンティティをまたぐ検証 |

### Query / Command Patterns

| ID | パターン名 | 目的 |
|---|----------|------|
| `query-get-list` | GetListQuery | 全件取得 |
| `query-get-by-id` | GetByIdQuery | ID指定取得 |
| `command-create` | CreateCommand | エンティティ作成 |

---

## ディレクトリ構造

```
catalog/
├── README.md                 # このファイル
├── COMMON_MISTAKES.md        # 頻出ミス集
├── AI_USAGE_GUIDE.md         # AI向け利用ガイド
├── index.json                # パターン索引（マスター）
├── patterns/                 # Pipeline Behaviors, Domain Patterns
├── features/                 # Feature Slices
├── kernel/                   # Kernel Patterns
├── scaffolds/                # プロジェクト構造テンプレート
├── planning/                 # 計画フェーズガイド
├── implementation/           # 実装フェーズガイド
└── speckit-extensions/       # spec-kit統合
```

---

## クイックスタート

### 1. NuGetパッケージをインストール

```bash
dotnet add package VSA.Kernel
dotnet add package VSA.Application
dotnet add package VSA.Infrastructure
```

### 2. DI登録

```csharp
// Program.cs
services.AddVsaFramework(typeof(Program).Assembly);
```

### 3. パターンに従って実装

```csharp
// CreateTaskCommand.cs
public sealed record CreateTaskCommand(
    string Title,
    string? Description
) : ICommand<Result<TaskId>>;

// CreateTaskHandler.cs
public sealed class CreateTaskHandler
    : CreateEntityHandler<CreateTaskCommand, TaskItem, TaskId>
{
    // VSA.Handlers の汎用ハンドラーを継承
}
```

---

## AIによる利用

### 推奨プロンプト

```
このプロジェクトには catalog/ ディレクトリにパターンカタログがあります。
VSA.Framework NuGetパッケージを使用しています。

新機能を実装する際は:
1. catalog/index.json を読み込み、適切なパターンを検索
2. 該当パターンの YAML ファイルを読み込み
3. VSA.Framework の基底クラスを継承
4. ai_guidance の common_mistakes を確認
```

詳細は [AI_USAGE_GUIDE.md](AI_USAGE_GUIDE.md) を参照。

---

## ランタイム要件

| 項目 | 値 |
|------|-----|
| **ターゲットフレームワーク** | `net9.0` |
| **必要なSDKバージョン** | `9.0.100` 以上 |
| **VSA.Framework** | `1.3.0` 以上 |

---

**最終更新: 2025-12-28**
**カタログバージョン: v2025.12.28**
