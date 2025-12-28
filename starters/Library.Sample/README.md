# Library.Sample - 図書館貸出管理システム

VSA.Frameworkを使用した**図書館貸出管理システム**のサンプル実装です。
複数の集約とビジネスルールを含む、実践的なドメインを示しています。

## 概要

このサンプルは、以下のドメインモデルを実装しています:

| エンティティ | 説明 |
|-------------|------|
| **Book** | 図書タイトル（書誌情報） |
| **BookCopy** | 蔵書コピー（実物の1冊） |
| **Member** | 利用者 |
| **Loan** | 貸出記録 |
| **Reservation** | 予約 |

### 実装済み機能

| 機能 | Command | ビジネスルール |
|------|---------|--------------|
| 貸出 | `LendCopyCommand` | Available状態のコピーのみ、Active会員のみ、予約者優先 |
| 返却 | `ReturnCopyCommand` | 返却後、予約があれば先頭をReady状態に |
| 予約 | `ReserveBookCommand` | 利用可能コピーがない場合のみ、Book単位で先着順 |

## ドメインモデル

```
┌─────────────────────────────────────────────────────────────┐
│                     Library BC                               │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────┐       ┌──────────┐       ┌────────────┐       │
│  │   Book   │ 1:N   │ BookCopy │ 1:1   │    Loan    │       │
│  │ (Title)  │──────▶│ (実物)   │◀──────│  (貸出)    │       │
│  └──────────┘       └──────────┘       └────────────┘       │
│       │                                      ▲              │
│       │ 1:N                                  │              │
│       ▼                                      │              │
│  ┌────────────┐     ┌──────────┐             │              │
│  │Reservation │     │  Member  │─────────────┘              │
│  │  (予約)    │◀────│ (利用者) │                            │
│  └────────────┘     └──────────┘                            │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## 主要なビジネスルール

### 1. 貸出ルール

- `BookCopy.Status == Available` のときのみ貸出可能
- `Member.Status == Active` のときのみ貸出可能
- 予約がある場合、`Ready` 状態の予約者のみ貸出可能
- 貸出期間は14日間

### 2. 返却ルール

- 返却後、予約があれば先頭を `Ready` 状態にする
- 予約があれば `BookCopy.Status = Reserved` に変更
- 予約がなければ `BookCopy.Status = Available` に変更

### 3. 予約ルール

- `Book` の利用可能なコピーが **1冊もない** 場合のみ予約可能
- 予約はBook単位（タイトル単位）で行う
- 先着順（Position）で管理

## VSA.Framework パターンの適用

### Boundaryパターン

各エンティティに `CanXxx()` メソッドを実装:

```csharp
// Book.cs
public BoundaryDecision CanReserve()
{
    if (_copies.Any(c => c.Status == CopyStatus.Available))
        return BoundaryDecision.Deny("貸出可能なコピーがあるため予約できません");
    return BoundaryDecision.Allow();
}

// Member.cs
public BoundaryDecision CanBorrow()
{
    if (Status == MemberStatus.Suspended)
        return BoundaryDecision.Deny("貸出停止中のため借りることができません");
    return BoundaryDecision.Allow();
}
```

### Result<T>パターン

Handlerは例外ではなく `Result<T>` でエラーを返す:

```csharp
var decision = copy.CanLend(readyReservationMemberId, borrowingMemberId);
if (!decision.IsAllowed)
    return Result.Fail<LoanId>(decision.Reason!);

return Result.Success(loanId);
```

### TransactionBehavior

Handler内で `SaveChangesAsync` を呼ばない:

```csharp
// ❌ 禁止
await _dbContext.SaveChangesAsync(ct);

// ✅ 正しい: Pipeline Behaviorが自動実行
return Result.Success(loanId);
```

## プロジェクト構造

```
src/
├── Domain/Library/           # ドメイン層
│   ├── Books/                # Book, BookCopy
│   ├── Members/              # Member
│   ├── Loans/                # Loan
│   └── Reservations/         # Reservation
│
└── Application/Features/     # アプリケーション層
    ├── LendCopy/             # 貸出
    ├── ReturnCopy/           # 返却
    └── ReserveBook/          # 予約
```

## セットアップ

```bash
# NuGetパッケージのインストール
dotnet add package VSA.Kernel
dotnet add package VSA.Application
dotnet add package VSA.Infrastructure
dotnet add package FluentValidation
```

## 機能追加の参考

このサンプルをベースに以下の機能を追加できます:

| 機能 | 参考パターン |
|------|-------------|
| Book登録 | `catalog/features/feature-create-entity.yaml` |
| Member登録 | `catalog/features/feature-create-entity.yaml` |
| Book検索 | `catalog/features/feature-search-entity.yaml` |
| 延滞一覧 | `catalog/features/feature-search-entity.yaml` |
| 予約キャンセル | `catalog/features/feature-delete-entity.yaml` |

## 仕様書

このサンプルは以下の仕様書に基づいています:
- 図書館貸出管理システム要求仕様（AI実装向け）

---

**VSA.Framework v1.3.0 | .NET 9.0**
