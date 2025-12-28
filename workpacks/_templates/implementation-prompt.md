# Implementation Prompt Template

このテンプレートは、workpackからunified diffを生成するためのプロンプトです。

---

## Phase 0: Mandatory Catalog Reading (CRITICAL)

**This phase is MANDATORY. Do NOT skip.**

### Step 0.1: Read VSA.Framework Catalog

Read the following from `catalog/`:

1. **catalog/index.json** - パターン索引
2. **catalog/COMMON_MISTAKES.md** - 頻出ミス（必読）
3. **catalog/AI_USAGE_GUIDE.md** - AI利用ガイド
4. 該当パターンのYAMLファイル

### Step 0.2: Quote Catalog Constraints

各パターンから `must_read_checklist` または `ai_guidance.common_mistakes` を引用:

```markdown
## Implementation Notes - Catalog Constraints

### From transaction-behavior.yaml

**Checklist:**
> - Handler 内で SaveChangesAsync を呼ばない
> - TransactionBehavior に任せる

**How it affects this feature:**
- Handler で SaveChangesAsync を呼ばない
- return Result.Success(entity.Id) で終了

### From validation-behavior.yaml

**Checklist:**
> - DBアクセスを伴う検証は Validator に書かない

**How it affects this feature:**
- Validator は形式検証のみ
- 存在確認は Handler で実施

### From boundary-pattern.yaml

**Checklist:**
> - Entity.CanXxx() は BoundaryDecision を返す
> - Handler は CanXxx() の結果を必ずチェック

**How it affects this feature:**
- Entity に CanUpdate(), CanDelete() を実装
- Handler で BoundaryDecision をチェック
```

### Step 0.3: Quote COMMON_MISTAKES.md

```markdown
### From COMMON_MISTAKES.md
> - Handler内でSaveChangesAsync()を呼ばない（TransactionBehaviorが自動実行）
> - BoundaryServiceに業務ロジック（if文）を書かない
> - Value Objectの比較はインスタンス同士で行う
```

---

## Phase 1: Implementation Plan

### Step 1.1: List Files to Create/Modify

```markdown
## Implementation Plan

### Files to Create

| File | Responsibility | Patterns Applied | COMMON_MISTAKES to Avoid |
|------|---------------|------------------|-------------------------|
| TaskId.cs | 型付きID | domain-typed-id | ITypedIdを実装 |
| TaskItem.cs | 集約ルート + CanXxx() | entity-base, boundary-pattern | BoundaryDecisionを返す |
| CreateTaskCommand.cs | コマンド定義 | feature-create-entity | ICommand<Result<T>>を実装 |
| CreateTaskHandler.cs | 作成処理 | feature-create-entity | SaveChangesAsyncを呼ばない |
| CreateTaskValidator.cs | 形式検証 | validation-behavior | DBアクセスしない |

### Files to Modify

| File | Change | Reason |
|------|--------|--------|
| Program.cs | DI登録追加 | Validator, Handler |
```

---

## Phase 2: Implementation

For each file, follow the patterns from catalog YAML templates.

### VSA.Framework Base Classes to Use

| Task | Base Class | Package |
|------|-----------|---------|
| Entity creation | `AggregateRoot<TId>` | VSA.Kernel |
| Typed ID | `ITypedId` | VSA.Kernel |
| Command | `ICommand<Result<T>>` | VSA.Application |
| Handler | `CreateEntityHandler<T>` | VSA.Handlers |
| Validator | `AbstractValidator<T>` | FluentValidation |

---

## Phase 3: Verification

After implementation, verify against catalog checklist:

```markdown
## Post-Implementation Verification

| Quote Item | Verified? | Evidence |
|------------|:---------:|----------|
| SaveChangesAsync not in Handler | ✅ | Handler.cs does not call SaveChangesAsync |
| Entity.CanXxx() returns BoundaryDecision | ✅ | TaskItem.cs:CanUpdate(), CanDelete() |
| ITypedId implemented | ✅ | TaskId.cs implements ITypedId |
```

---

## Key Rules

- **NEVER skip Phase 0** - quotes prove catalog was read
- **ALWAYS use VSA.Framework base classes** - don't reinvent
- **ALWAYS document quotes** before starting implementation
- Quotes should be specific and actionable

---

## Output Format

Output should be **unified diff** format only:

```diff
--- a/src/Domain/TaskManagement/TaskId.cs
+++ b/src/Domain/TaskManagement/TaskId.cs
@@ -0,0 +1,15 @@
+using VSA.Kernel;
+
+namespace Domain.TaskManagement;
+
+public readonly record struct TaskId(Guid Value) : ITypedId
+{
+    public static TaskId New() => new(Guid.NewGuid());
+    public static TaskId From(Guid value) => new(value);
+}
```
