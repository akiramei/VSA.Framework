# VSA.Infrastructure - Pipeline Behaviorスキル

VSA.Infrastructureを使用したバリデーション、ロギング、トランザクション等のガイドです。

## DI登録

```csharp
// Program.cs
services.AddVsaInfrastructure();

// これにより以下のPipeline Behaviorsが登録されます:
// 1. ValidationBehavior - FluentValidationによる入力検証
// 2. LoggingBehavior - リクエスト/レスポンスのログ出力
// 3. TransactionBehavior - コマンドのトランザクション管理
// 4. CachingBehavior - クエリ結果のキャッシュ
// 5. ExceptionHandlingBehavior - 例外の統一処理
// 6. PerformanceBehavior - 実行時間の監視
// 7. AuthorizationBehavior - 認可チェック
// 8. AuditLogBehavior - 監査ログ記録
// 9. IdempotencyBehavior - 冪等性の保証
```

## ValidationBehavior（バリデーション）

FluentValidationを使用した入力検証を自動実行します。

```csharp
// Validator定義
public class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("タイトルは必須です")
            .MaximumLength(200).WithMessage("タイトルは200文字以内で入力してください");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("著者は必須です")
            .MaximumLength(100).WithMessage("著者は100文字以内で入力してください");

        RuleFor(x => x.Isbn)
            .Matches(@"^\d{13}$").When(x => !string.IsNullOrEmpty(x.Isbn))
            .WithMessage("ISBNは13桁の数字で入力してください");
    }
}

// 非同期バリデーション（DBチェックなど）
public class CreateMemberCommandValidator : AbstractValidator<CreateMemberCommand>
{
    private readonly IMemberRepository _repository;

    public CreateMemberCommandValidator(IMemberRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("メールアドレスは必須です")
            .EmailAddress().WithMessage("有効なメールアドレスを入力してください")
            .MustAsync(BeUniqueEmail).WithMessage("このメールアドレスは既に使用されています");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken ct)
    {
        return !await _repository.ExistsByEmailAsync(email, ct);
    }
}

// DI登録
services.AddValidatorsFromAssembly(typeof(CreateBookCommandValidator).Assembly);
```

## LoggingBehavior（ロギング）

リクエストの開始/終了、実行時間を自動ログ出力します。

```
[2024-01-15 10:30:15] INFO  Handling CreateBookCommand
[2024-01-15 10:30:15] INFO  Handled CreateBookCommand in 45ms
```

設定不要で自動的に動作します。

## TransactionBehavior（トランザクション）

Commandの実行をトランザクションで囲みます。

```csharp
// ITransactionProviderを実装してDIに登録
public class EfTransactionProvider : ITransactionProvider
{
    private readonly AppDbContext _context;

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await operation();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}

// DI登録
services.AddScoped<ITransactionProvider, EfTransactionProvider>();
```

## CachingBehavior（キャッシュ）

ICacheableQueryを実装したクエリの結果をキャッシュします。

```csharp
// キャッシュ可能なクエリ
public record GetBookCategoriesQuery
    : IQuery<Result<IReadOnlyList<CategoryDto>>>, ICacheableQuery
{
    public string CacheKey => "book-categories";
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);
}

// パラメータ付きキャッシュキー
public record GetBookByIdQuery(Guid BookId)
    : IQuery<Result<BookDto>>, ICacheableQuery
{
    public string CacheKey => $"book:{BookId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
}

// DI登録（メモリキャッシュ）
services.AddMemoryCache();
```

## AuthorizationBehavior（認可）

Authorize属性を持つコマンド/クエリの認可チェックを行います。

```csharp
// Authorize属性付きコマンド
[Authorize(Roles = "Admin")]
public record DeleteBookCommand(Guid BookId) : ICommand<Result>;

[Authorize(Roles = "Admin,Librarian")]
public record UpdateMemberCommand(...) : ICommand<Result<MemberDto>>;

[Authorize(Policy = "CanManageLoans")]
public record ReturnBookCommand(Guid LoanId) : ICommand<Result>;

// ICurrentUserServiceを実装
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value)
        ?? Enumerable.Empty<string>();

    public bool IsInRole(string role) => Roles.Contains(role);
}

// DI登録
services.AddScoped<ICurrentUserService, CurrentUserService>();
```

## AuditLogBehavior（監査ログ）

IAuditableCommandを実装したコマンドの監査ログを記録します。

```csharp
// 監査対象コマンド
public record CreateBookCommand(string Title, string Author)
    : ICommand<Result<Guid>>, IAuditableCommand
{
    public string AuditAction => "CreateBook";
    public object AuditData => new { Title, Author };
}

public record DeleteMemberCommand(Guid MemberId)
    : ICommand<Result>, IAuditableCommand
{
    public string AuditAction => "DeleteMember";
    public object AuditData => new { MemberId };
}

// IAuditLogServiceを実装
public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;

    public async Task LogAsync(
        string action,
        string userId,
        object data,
        CancellationToken ct)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Action = action,
            UserId = userId,
            Data = JsonSerializer.Serialize(data),
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(ct);
    }
}

// DI登録
services.AddScoped<IAuditLogService, AuditLogService>();
```

## IdempotencyBehavior（冪等性）

IIdempotentCommandを実装したコマンドの重複実行を防止します。

```csharp
// 冪等コマンド
public record ProcessPaymentCommand(Guid OrderId, decimal Amount)
    : ICommand<Result<Guid>>, IIdempotentCommand
{
    public string IdempotencyKey => $"payment:{OrderId}";
}

public record SendNotificationCommand(Guid UserId, string Message)
    : ICommand<Result>, IIdempotentCommand
{
    public string IdempotencyKey => $"notification:{UserId}:{Message.GetHashCode()}";
}

// IIdempotencyStoreを実装
public class IdempotencyStore : IIdempotencyStore
{
    private readonly IDistributedCache _cache;

    public async Task<bool> ExistsAsync(string key, CancellationToken ct)
    {
        var value = await _cache.GetStringAsync(key, ct);
        return value != null;
    }

    public async Task SetAsync(string key, TimeSpan expiry, CancellationToken ct)
    {
        await _cache.SetStringAsync(key, "1", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry
        }, ct);
    }

    public async Task<T?> GetResultAsync<T>(string key, CancellationToken ct)
    {
        var value = await _cache.GetStringAsync(key, ct);
        return value != null ? JsonSerializer.Deserialize<T>(value) : default;
    }
}

// DI登録
services.AddScoped<IIdempotencyStore, IdempotencyStore>();
```

## PerformanceBehavior（パフォーマンス監視）

実行時間が閾値を超えた場合に警告ログを出力します。

```
[2024-01-15 10:30:15] WARN  GetBooksQuery took 1523ms (threshold: 500ms)
```

設定不要で自動的に動作します（デフォルト閾値: 500ms）。

## Pipeline実行順序

```
Request
   │
   ▼
┌─────────────────────────┐
│ 1. LoggingBehavior      │ ログ開始
├─────────────────────────┤
│ 2. PerformanceBehavior  │ 計測開始
├─────────────────────────┤
│ 3. ExceptionHandling    │ 例外キャッチ準備
├─────────────────────────┤
│ 4. ValidationBehavior   │ 入力検証
├─────────────────────────┤
│ 5. AuthorizationBehavior│ 認可チェック
├─────────────────────────┤
│ 6. IdempotencyBehavior  │ 重複チェック
├─────────────────────────┤
│ 7. CachingBehavior      │ キャッシュ確認
├─────────────────────────┤
│ 8. TransactionBehavior  │ トランザクション開始
├─────────────────────────┤
│ 9. AuditLogBehavior     │ 監査ログ準備
├─────────────────────────┤
│      Handler実行        │ ビジネスロジック
├─────────────────────────┤
│ 9. AuditLogBehavior     │ 監査ログ記録
├─────────────────────────┤
│ 8. TransactionBehavior  │ コミット/ロールバック
├─────────────────────────┤
│ 7. CachingBehavior      │ 結果キャッシュ
├─────────────────────────┤
│ ...                     │ 逆順で戻る
└─────────────────────────┘
   │
   ▼
Response
```
