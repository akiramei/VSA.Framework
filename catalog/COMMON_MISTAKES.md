# Common Mistakes - 実装前に必ず読むこと

**このファイルは、AIおよび開発者が陥りやすい実装ミスをまとめたものです。**

VSA.Frameworkを使用する際、このドキュメントを一読してください。

---

## 絶対禁止事項（NEVER DO）

### 1. Handler内でSaveChangesAsync()を呼ばない

```csharp
// ❌ 禁止: 二重保存の原因
public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
{
    var entity = new Product(...);
    await _repository.AddAsync(entity, ct);
    await _dbContext.SaveChangesAsync(ct);  // ← これを書かない！
    return Result.Success(entity.Id);
}

// ✅ 正しい: TransactionBehaviorが自動でSaveChangesAsyncを呼ぶ
public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
{
    var entity = new Product(...);
    await _repository.AddAsync(entity, ct);
    return Result.Success(entity.Id);  // SaveChangesAsyncは不要
}
```

**理由**: VSA.Infrastructureの`TransactionBehavior`（order: 400）がHandlerの実行後に自動で`SaveChangesAsync`を呼び出します。

---

### 2. SingletonでDbContextやScopedサービスを注入しない

```csharp
// ❌ 禁止: Captive Dependency問題
services.AddSingleton<IMyService, MyService>();  // SingletonがDbContextを持つ

// ✅ 正しい: すべてScopedで統一
services.AddScoped<IMyService, MyService>();
```

**理由**: MediatRはScopedで動作するため、Singletonサービスがスコープ付きの依存関係を持つと問題が発生します。

---

### 3. MediatRのHandleメソッド名をHandleAsyncにしない

```csharp
// ❌ 禁止: MediatRの規約外
public async Task<Result<Guid>> HandleAsync(...)  // Asyncが付いている

// ✅ 正しい: メソッド名は Handle
public async Task<Result<Guid>> Handle(...)
```

**理由**: MediatRは`Handle`という名前のメソッドを探します。

---

### 4. 例外をthrowしてエラーを伝播しない

```csharp
// ❌ 禁止: 例外による制御フロー
if (product == null)
    throw new NotFoundException("Product not found");

// ✅ 正しい: VSA.ApplicationのResult<T>パターンを使用
if (product == null)
    return Result.Fail<Product>("Product not found");
```

**理由**: 例外は本当に予期しないエラーのみに使用します。ビジネスロジック上のエラーは`Result<T>`で明示的に伝播します。

---

## EF Core トラッキング問題（CRITICAL）

### AsNoTracking で取得したエンティティの状態変更は保存されない

```csharp
// ❌ 致命的バグ: AsNoTracking で取得したエンティティを変更
var copy = await _dbContext.BookCopies
    .AsNoTracking()  // ← 非トラッキング
    .FirstOrDefaultAsync(c => c.Id == id, ct);

copy.MarkAsOnLoan();  // ← 状態変更しても...
// SaveChangesAsync しても DB に反映されない！

// ✅ 正しい: 更新用クエリは AsNoTracking を使わない
var copy = await _dbContext.BookCopies
    // AsNoTracking なし = トラッキングされる
    .FirstOrDefaultAsync(c => c.Id == id, ct);
```

---

## EF Core + Value Object の比較

Value Objectの比較は**インスタンス同士**で行ってください。

```csharp
// ✅ 正しい: Value Objectインスタンスで比較
var boardId = BoardId.From(guid);
var board = await _dbContext.Boards
    .Where(b => b.Id == boardId)
    .FirstOrDefaultAsync();

// ❌ LINQ変換エラー: .Value プロパティにアクセス
var board = await _dbContext.Boards
    .Where(b => b.Id.Value == guid)  // EF CoreがLINQに変換できない
    .FirstOrDefaultAsync();
```

---

## Boundary判定（操作可否のビジネスロジック）

操作の実行可否判定は**Entity.CanXxx()メソッド**で行い、BoundaryDecision（VSA.Kernel）を使用してください。

```csharp
// ✅ 正しい: Entity.CanXxx()で判定
public class Order : AggregateRoot<OrderId>
{
    public BoundaryDecision CanPay()
    {
        return Status switch
        {
            OrderStatus.Pending => BoundaryDecision.Allow(),
            OrderStatus.Paid => BoundaryDecision.Deny("既に支払い済みです"),
            _ => BoundaryDecision.Deny("この状態では支払いできません")
        };
    }
}

// Handler側
var decision = order.CanPay();
if (!decision.IsAllowed)
    return Result.Fail(decision.Reason);

// ❌ 禁止: UIにビジネスロジックを記述
@if (order.Status == OrderStatus.Paid)
{
    <button disabled>支払い不可</button>  // UIが業務ルールを知っている
}
```

---

## VSA.Frameworkの基底クラスを使用する

### VSA.Kernelの基底クラス

```csharp
// ✅ 正しい: VSA.Kernelの基底クラスを継承
using VSA.Kernel;

public class Product : AggregateRoot<ProductId>
{
    // ...
}

public readonly record struct ProductId(Guid Value) : ITypedId;

// ❌ 禁止: 独自の基底クラスを作る
public abstract class MyEntity { }  // 使わない
```

### VSA.Handlersの汎用ハンドラー

```csharp
// ✅ 正しい: VSA.Handlersの汎用ハンドラーを継承
using VSA.Handlers.Commands;

public class CreateProductHandler
    : CreateEntityHandler<CreateProductCommand, Product, ProductId>
{
    // カスタマイズが必要な場合のみオーバーライド
}

// ❌ 禁止: 毎回フルスクラッチでHandlerを書く
```

### VSA.Blazorのページ基底クラス

```csharp
// ✅ 正しい: VSA.Blazorの基底クラスを継承
@inherits ListPageBase<GetProductsQuery, ProductDto>

// ❌ 禁止: 毎回ComponentBaseから書き直す
```

---

## FluentValidation（ValidationBehavior）の範囲

### DBアクセスを伴う検証は ValidationBehavior でやらない

```csharp
// ❌ 禁止: Validator内でDBアクセス
public class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator(IBookingRepository repo)
    {
        RuleFor(x => x.RoomId)
            .MustAsync(async (roomId, ct) => await repo.ExistsAsync(roomId, ct))  // ← DBアクセス
            .WithMessage("会議室が存在しません");
    }
}

// ✅ 正しい: 形式検証のみ
public class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime);  // 形式検証
    }
}

// 存在確認はHandler内で
public async Task<Result<Guid>> Handle(CreateBookingCommand request, CancellationToken ct)
{
    var room = await _roomRepository.GetByIdAsync(request.RoomId, ct);
    if (room is null)
        return Result.Fail<Guid>("会議室が存在しません");
    // ...
}
```

**検証の分担**:

| 検証内容 | 配置場所 |
|---------|---------|
| 入力値の形式（空文字、長さ、範囲） | ValidationBehavior（FluentValidation） |
| データの存在確認 | Handler内 |
| ビジネスルール（重複チェック等） | ドメインサービス |

---

## 実装前チェックリスト

```
□ VSA.Framework NuGetパッケージを参照しているか？
□ catalog/index.json を読んだか？
□ 該当パターンの YAML を読んだか？
□ Handler内でSaveChangesAsyncを呼んでいないか？
□ すべてのサービスはScopedで登録しているか？
□ Value Objectの比較はインスタンス同士で行っているか？
□ 操作可否判定はEntity.CanXxx()とBoundaryDecisionで行っているか？
□ VSA.Kernelの基底クラス（AggregateRoot, Entity, ValueObject）を使用しているか？
□ FluentValidationはDBアクセスを伴わない形式検証のみにしているか？
```

---

## ケアレスミス集

### 1. Query/Command の引数順序の誤り

```csharp
// ❌ 誤り: 引数順序を推測で書いた
await Mediator.Send(new GetBooksQuery(true));

// ✅ 正しい: 名前付き引数を使用
await Mediator.Send(new GetBooksQuery(searchTerm: null, includeInactive: true));
```

### 2. DTO プロパティ名の不一致

```csharp
// ❌ 誤り: Entity のプロパティ名を推測で使用
<h1>@_member.MemberName</h1>

// ✅ 正しい: DTO のプロパティ名を確認
<h1>@_member.Name</h1>
```

**対策**:
- DTO の定義を必ず確認してからUIを実装する
- IDE の補完機能を活用する
- 名前付き引数を使用する

---

## 関連ドキュメント

- [AI_USAGE_GUIDE.md](AI_USAGE_GUIDE.md) - 詳細な実装ガイド
- [README.md](README.md) - パターンカタログ概要
- VSA.Framework Skills: `.claude/skills/vsa-*.md`

---

**最終更新: 2025-12-28**
**カタログバージョン: v2025.12.28**
