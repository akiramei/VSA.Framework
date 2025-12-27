# VSA.Kernel - DDD基盤スキル

VSA.Kernelを使用したドメインモデル作成のガイドです。

## TypedId（型付きID）

強く型付けされたIDを定義します。`readonly record struct`で実装し、`ITypedId`を実装します。

```csharp
// 推奨パターン: readonly record struct
public readonly record struct BookId(Guid Value) : ITypedId;
public readonly record struct MemberId(Guid Value) : ITypedId;
public readonly record struct OrderId(Guid Value) : ITypedId;

// 使用例
var bookId = new BookId(Guid.NewGuid());
if (bookId.IsValid()) { /* 有効なID */ }
if (bookId.IsEmpty()) { /* 空のID */ }
```

## Entity（エンティティ）

IDで識別されるドメインオブジェクトです。

```csharp
public class Book : Entity<BookId>
{
    public string Title { get; private set; } = default!;
    public string Author { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    // privateコンストラクタ + ファクトリメソッド
    private Book() { }

    public static Book Create(string title, string author)
    {
        return new Book
        {
            Id = new BookId(Guid.NewGuid()),
            Title = title,
            Author = author,
            IsActive = true
        };
    }

    // ドメインロジックをメソッドとして実装
    public void UpdateTitle(string newTitle)
    {
        Title = newTitle;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
```

## AggregateRoot（集約ルート）

集約の境界を定義し、ドメインイベントを発行できます。

```csharp
public class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLine> _lines = new();
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public OrderStatus Status { get; private set; }

    public static Order Create(MemberId memberId)
    {
        var order = new Order
        {
            Id = new OrderId(Guid.NewGuid()),
            Status = OrderStatus.Draft
        };

        // ドメインイベントを発行
        order.AddDomainEvent(new OrderCreatedEvent(order.Id, memberId));
        return order;
    }

    public void AddLine(BookId bookId, int quantity)
    {
        _lines.Add(new OrderLine(bookId, quantity));
        AddDomainEvent(new OrderLineAddedEvent(Id, bookId, quantity));
    }
}
```

## ValueObject（値オブジェクト）

値で比較されるイミュータブルなオブジェクトです。

```csharp
public class Address : ValueObject
{
    public string Prefecture { get; }
    public string City { get; }
    public string Street { get; }

    public Address(string prefecture, string city, string street)
    {
        Prefecture = prefecture;
        City = city;
        Street = street;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Prefecture;
        yield return City;
        yield return Street;
    }
}

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("通貨が異なります");
        return new Money(Amount + other.Amount, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

## BoundaryDecision（境界判定）

ドメイン操作の許可/拒否を明確に表現します。

```csharp
public class Member : Entity<MemberId>
{
    public MemberStatus Status { get; private set; }
    public int ActiveLoanCount { get; private set; }

    // 操作可否をBoundaryDecisionで返す
    public BoundaryDecision CanBorrow()
    {
        if (Status == MemberStatus.Suspended)
            return BoundaryDecision.Deny("会員資格が停止中です");

        if (ActiveLoanCount >= 5)
            return BoundaryDecision.Deny("貸出上限に達しています");

        return BoundaryDecision.Allow();
    }

    public BoundaryDecision CanDeactivate()
    {
        if (ActiveLoanCount > 0)
            return BoundaryDecision.Deny("未返却の本があります");

        return BoundaryDecision.Allow();
    }
}

// 使用例（Handler内）
public async Task<Result<Guid>> Handle(BorrowBookCommand request, CancellationToken ct)
{
    var member = await _memberRepository.GetByIdAsync(request.MemberId, ct);

    var canBorrow = member.CanBorrow();
    if (!canBorrow)  // 暗黙的にboolに変換
        return Result.Fail<Guid>(canBorrow.Reason!);

    // 貸出処理...
}
```

## DomainEvent（ドメインイベント）

ドメインで発生した重要な出来事を表現します。

```csharp
public record BookCreatedEvent(BookId BookId, string Title) : IDomainEvent;
public record BookBorrowedEvent(BookId BookId, MemberId MemberId, DateTime DueDate) : IDomainEvent;
public record BookReturnedEvent(BookId BookId, MemberId MemberId) : IDomainEvent;
```

## DomainException（ドメイン例外）

ドメインルール違反を表現する例外です。

```csharp
public class BookNotAvailableException : DomainException
{
    public BookNotAvailableException(BookId bookId)
        : base($"図書 {bookId.Value} は現在貸出できません")
    {
    }
}

public class MemberSuspendedException : DomainException
{
    public MemberSuspendedException(MemberId memberId)
        : base($"会員 {memberId.Value} は資格停止中です")
    {
    }
}
```

## ディレクトリ構成例

```
src/
└── Domain/
    └── Books/
        ├── BookId.cs           # TypedId
        ├── Book.cs             # Entity/AggregateRoot
        ├── BookStatus.cs       # Enum
        ├── Isbn.cs             # ValueObject
        └── Events/
            ├── BookCreatedEvent.cs
            └── BookBorrowedEvent.cs
```
