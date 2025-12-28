# Workpacks - ステートレスAI駆動開発

## 概要

workpacksは、AI駆動開発における**再現性**を確保するための自己完結型入力パッケージです。

**問題**: 会話ベースのLLM利用はコンテキスト圧縮が非決定的で、再現性が低い

**解決策**: LLMを「ステートレス関数」として使用し、各タスクを自己完結型パッケージで実行

## アーキテクチャ

```
Phase A: 設計・高コンテキスト（考えるAI）
├── spec.yaml → decisions.yaml → guardrails.yaml → plan.md
└── 対話的、カタログ全体参照、パターン選択
        │
        ▼
    workpack（境界成果物）
        │
        ▼
Phase B: 実装・低コンテキスト（作るAI）
├── workpack → unified diff
└── 非対話的、純関数的、claude -p で実行
```

## VSA.Framework との統合

workpacksはVSA.Framework NuGetパッケージと連携して動作します。

| パッケージ | workpackでの役割 |
|-----------|-----------------|
| VSA.Kernel | Entity, AggregateRoot, BoundaryDecision のテンプレート |
| VSA.Application | ICommand, IQuery, Result<T> のテンプレート |
| VSA.Infrastructure | Pipeline Behaviors の自動適用 |
| VSA.Handlers | 汎用ハンドラーのテンプレート |

## ディレクトリ構造

```
workpacks/
├── README.md              # このファイル
├── _templates/            # workpack テンプレート
│   ├── task.template.md
│   ├── spec.extract.template.md
│   └── repo.snapshot.template.md
│
├── active/                # 進行中タスク
│   └── {task-id}/
│       ├── task.md              # コア: タスク定義
│       ├── spec.extract.md      # コア: 仕様抽出
│       ├── policy.yaml          # コア: ポリシー
│       ├── guardrails.yaml      # コア: ガードレール
│       └── repo.snapshot.md     # コア: リポジトリスナップショット
│
├── completed/             # 完了済み（履歴）
│   └── {task-id}/
│
└── failed/                # 失敗・保留
    └── {task-id}/
```

## workpack のファイル構成

### コア5ファイル（入力）

| ファイル | 役割 | 生成元 |
|---------|------|-------|
| task.md | タスク定義（What to do） | plan.md + tasks.yaml |
| spec.extract.md | 関連仕様抽出（Domain context） | spec.yaml |
| policy.yaml | 適用ポリシー（How to do） | decisions.yaml + manifest.yaml |
| guardrails.yaml | ガードレール（What NOT to do） | guardrails.yaml |
| repo.snapshot.md | 関連コード（Where to do） | 既存コードベース |

## 使用方法

### 1. workpack 生成

```powershell
# specs/ から workpack を生成
./scripts/generate-workpack.ps1 -TaskId "T001-create-task-entity" -SpecPath "specs/task/CreateTask"
```

### 2. ステートレス実装

```powershell
# claude -p でステートレス実行
./scripts/run-implementation.ps1 -TaskId "T001-create-task-entity"
```

### 3. diff 適用

```powershell
# 生成された diff をリポジトリに適用
./scripts/apply-diff.ps1 -TaskId "T001-create-task-entity"
```

## 設計原則

### 純関数的実行

```
f(workpack) → diff
```

- 入力: workpack（コア5ファイル）のみ
- 出力: unified diff のみ
- 副作用: なし（diff適用は別ステップ）

### 自己完結性

workpack 内のファイルだけで実装可能。外部参照不要。

### 再現性

同一 workpack → 同一 diff（を目指す）

## Phase A/B の責務分離

| 属性 | Phase A（設計） | Phase B（実装） |
|------|----------------|-----------------|
| 役割 | 考えるAI（Architect） | 作るAI（Coder） |
| 入力 | 自然言語、カタログ全体 | workpack のみ |
| 出力 | spec, decisions, guardrails, plan | unified diff |
| コンテキスト | 高（対話的） | 低（純関数的） |
| 再現性 | 中程度（LLMの非決定性あり） | 高 |

## 関連ドキュメント

- `catalog/AI_USAGE_GUIDE.md` - AI利用ガイド
- `catalog/COMMON_MISTAKES.md` - 頻出ミス集

---

**最終更新: 2025-12-28**
