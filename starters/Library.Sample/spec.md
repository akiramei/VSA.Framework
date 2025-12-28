# 図書館貸出管理システム – 要求仕様

## 1. システム概要

本システムは、小〜中規模の図書館で運用することを想定した、**基本的な貸出管理システム**である。

本システムでは：

* 「図書タイトル（Book）」と「蔵書コピー（BookCopy）」を区別する
* 利用者（Member）が蔵書コピーを借りる（Loan）、返す（Return）、予約する（Reservation）
* 貸出期限（DueDate）、延滞（Overdue）、予約の先着順などの一般的なルールを扱う

## 2. ドメインモデル

### 2.1 図書タイトル（Book）

書籍のタイトル情報。同名タイトルの蔵書コピーが複数存在しうる。

* 属性
  * `BookId`: 一意な識別子
  * `Title`: 書名
  * `Author`: 著者名
  * `Publisher`（任意）
  * `PublishedYear`（任意）
  * `IsActive`: 利用可否

### 2.2 蔵書コピー（BookCopy）

実物としての1冊を表す。Book と 1:N の関係。

* 属性
  * `CopyId`: 一意な識別子
  * `BookId`
  * `CopyNumber`: BookId内での冊番号
  * `Status`: `Available` / `OnLoan` / `Reserved` / `Inactive`
  * `Location`（任意）
  * `Notes`（任意）

### 2.3 利用者（Member）

* 属性
  * `MemberId`: 一意な識別子
  * `Name`: 利用者名
  * `Email`（任意）
  * `Phone`（任意）
  * `Status`: `Active` / `Suspended`

### 2.4 貸出（Loan）

* 属性
  * `LoanId`
  * `CopyId`
  * `MemberId`
  * `LoanDate`: 貸出日
  * `DueDate`: 返却期限（LoanDate + 14日）
  * `ReturnDate`（任意）: 返却時に設定
  * `Status`: `OnLoan` / `Returned` / `Overdue`

### 2.5 予約（Reservation）

予約はBook単位（タイトル単位）で行う。

* 属性
  * `ReservationId`
  * `BookId`
  * `MemberId`
  * `ReservedAt`: 予約日時
  * `Status`: `Waiting` / `Ready` / `Cancelled`
  * `Position`: 予約の順番（先着順）

## 3. 機能要件

### 3.1 貸出の実行

**入力**: `CopyId`, `MemberId`
**処理**:
1. `CopyId` が `Available` であること
2. Member が `Active` であること
3. Loan レコード作成
4. BookCopy の `Status = OnLoan` に変更

### 3.2 返却

**入力**: `LoanId`
**処理**:
1. Loan 状態が `OnLoan` または `Overdue` であること
2. Loan の `Status = Returned` に変更
3. BookCopy を `Available` に戻す
4. 予約が存在する場合、先頭を `Ready` に変更

### 3.3 予約

**入力**: `BookId`, `MemberId`
**条件**:
1. Member が Active であること
2. Book の利用可能なコピーが **1冊もない** 場合のみ予約可能

## 4. ビジネスルール

1. コピーは同時に2人に貸出されてはならない
2. 予約は先着順（Position）で処理される
3. Ready状態の予約者に優先権がある
4. 貸出期限を過ぎた未返却は延滞（Overdue）

## 5. 代表的ユースケース

1. `LendCopy(copyId, memberId)` - 貸出
2. `ReturnCopy(loanId)` - 返却
3. `ReserveBook(bookId, memberId)` - 予約
4. `CancelReservation(reservationId)` - 予約キャンセル
