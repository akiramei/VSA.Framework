# Library.Sample - Claude Code ガイド

## プロジェクト概要

このプロジェクトは **VSA.Framework** を使用した **図書館貸出管理システム** のサンプル実装です。
複数の集約とビジネスルールを含む、実践的なドメインを示しています。

## ドメインモデル

```
Library BC (Bounded Context)
├── Books/           # 図書タイトル + 蔵書コピー
│   ├── Book         # 集約ルート（タイトル情報）
│   └── BookCopy     # エンティティ（実物の1冊）
│
├── Members/         # 利用者
│   └── Member       # 集約ルート
│
├── Loans/           # 貸出
│   └── Loan         # 集約ルート
│
└── Reservations/    # 予約
    └── Reservation  # 集約ルート
```

## 技術スタック

| 技術 | バージョン | 用途 |
|------|----------|------|
| .NET | 9.0 | ランタイム |
| VSA.Kernel | 1.3.0 | Entity, AggregateRoot, BoundaryDecision, ITypedId |
| VSA.Application | 1.3.0 | ICommand, IQuery, Result<T> |
| VSA.Infrastructure | 1.3.0 | Pipeline Behaviors |
| FluentValidation | 12.0.0 | 入力検証 |
| MediatR | 12.0.0 | CQRS |

## 主要なビジネスルール

### 貸出（Loan）

1. `BookCopy.Status == Available` のときのみ貸出可能
2. `Member.Status == Active` のときのみ貸出可能
3. 予約がある場合、`Ready` 状態の予約者のみ貸出可能
4. 貸出期間は14日間

### 返却（Return）

1. `Loan.Status == OnLoan` または `Overdue` のときのみ返却可能
2. 返却後、予約があれば先頭を `Ready` 状態にする
3. 予約があれば `BookCopy.Status = Reserved` に変更

### 予約（Reservation）

1. `Book` の利用可能なコピーが **1冊もない** 場合のみ予約可能
2. `Member.Status == Active` のときのみ予約可能
3. 予約はBook単位（タイトル単位）で行う
4. 先着順（Position）で管理

## Boundaryパターンの適用例

### BookCopy.CanLend()

```csharp
public BoundaryDecision CanLend(MemberId? readyReservationMemberId, MemberId borrowingMemberId)
{
    if (Status == CopyStatus.OnLoan)
        return BoundaryDecision.Deny("このコピーは既に貸出中です");

    if (Status == CopyStatus.Reserved)
    {
        if (readyReservationMemberId != borrowingMemberId)
            return BoundaryDecision.Deny("予約者に優先権があります");
    }

    return BoundaryDecision.Allow();
}
```

### Book.CanReserve()

```csharp
public BoundaryDecision CanReserve()
{
    if (!IsActive)
        return BoundaryDecision.Deny("非アクティブな図書は予約できません");

    // 利用可能なコピーがあれば予約不可（直接借りられる）
    if (_copies.Any(c => c.Status == CopyStatus.Available))
        return BoundaryDecision.Deny("貸出可能なコピーがあるため予約できません");

    return BoundaryDecision.Allow();
}
```

## Handler の実装パターン

### LendCopyHandler（複数集約をまたがる操作）

```csharp
public async Task<Result<LoanId>> Handle(LendCopyCommand command, CancellationToken ct)
{
    // 1. 各エンティティを取得
    var copy = await _copyRepository.GetByIdAsync(command.CopyId, ct);
    var member = await _memberRepository.GetByIdAsync(command.MemberId, ct);

    // 2. Boundary で操作可否を判定
    var memberDecision = member.CanBorrow();
    if (!memberDecision.IsAllowed)
        return Result.Fail<LoanId>(memberDecision.Reason!);

    var copyDecision = copy.CanLend(readyReservationMemberId, command.MemberId);
    if (!copyDecision.IsAllowed)
        return Result.Fail<LoanId>(copyDecision.Reason!);

    // 3. Loan を作成
    var loan = Loan.Create(LoanId.New(), copy.Id, copy.BookId, command.MemberId);
    await _loanRepository.AddAsync(loan, ct);

    // 4. BookCopy のステータスを変更
    copy.MarkAsOnLoan();

    // SaveChangesAsync は呼ばない！
    return Result.Success(loan.Id);
}
```

## 禁止事項

### Handler内でSaveChangesAsyncを呼ばない

```csharp
// ❌ 禁止
await _dbContext.SaveChangesAsync(ct);

// ✅ 正しい: TransactionBehaviorが自動実行
return Result.Success(loan.Id);
```

### BoundaryDecisionを無視しない

```csharp
// ❌ 禁止: Boundaryをチェックせずに処理
copy.MarkAsOnLoan();

// ✅ 正しい: Boundaryで判定してから処理
var decision = copy.CanLend(readyReservationMemberId, borrowingMemberId);
if (!decision.IsAllowed)
    return Result.Fail<LoanId>(decision.Reason!);
copy.MarkAsOnLoan();
```

### 例外でエラーを伝播しない

```csharp
// ❌ 禁止
throw new InvalidOperationException("貸出中です");

// ✅ 正しい: Result<T>を使用
return Result.Fail<LoanId>("貸出中です");
```

## ファイル構成

```
src/
├── Domain/Library/
│   ├── Books/
│   │   ├── BookId.cs         # ITypedId
│   │   ├── CopyId.cs         # ITypedId
│   │   ├── CopyStatus.cs     # enum
│   │   ├── Book.cs           # AggregateRoot + Boundary
│   │   └── BookCopy.cs       # Entity + Boundary
│   ├── Members/
│   │   ├── MemberId.cs       # ITypedId
│   │   ├── MemberStatus.cs   # enum
│   │   └── Member.cs         # AggregateRoot + Boundary
│   ├── Loans/
│   │   ├── LoanId.cs         # ITypedId
│   │   ├── LoanStatus.cs     # enum
│   │   └── Loan.cs           # AggregateRoot + Boundary
│   └── Reservations/
│       ├── ReservationId.cs  # ITypedId
│       ├── ReservationStatus.cs # enum
│       └── Reservation.cs    # AggregateRoot + Boundary
│
└── Application/Features/
    ├── LendCopy/             # 貸出
    │   ├── LendCopyCommand.cs
    │   ├── LendCopyHandler.cs
    │   └── LendCopyValidator.cs
    ├── ReturnCopy/           # 返却
    │   ├── ReturnCopyCommand.cs
    │   └── ReturnCopyHandler.cs
    └── ReserveBook/          # 予約
        ├── ReserveBookCommand.cs
        └── ReserveBookHandler.cs
```

## カタログ参照

機能追加時は `../../catalog/` を参照:

1. `catalog/features/feature-create-entity.yaml` - エンティティ作成パターン
2. `catalog/patterns/boundary-pattern.yaml` - Boundaryパターン
3. `catalog/patterns/transaction-behavior.yaml` - トランザクション管理
4. `catalog/COMMON_MISTAKES.md` - 頻出ミス

---

**VSA.Framework v1.3.0**
