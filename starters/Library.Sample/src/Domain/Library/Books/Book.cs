using VSA.Kernel;

namespace Domain.Library.Books;

/// <summary>
/// 図書タイトル（集約ルート）
///
/// 書籍のタイトル情報を管理する。
/// 同名タイトルの蔵書コピーが複数存在しうる。
/// </summary>
public sealed class Book : AggregateRoot<BookId>
{
    private readonly List<BookCopy> _copies = [];

    public string Title { get; private set; } = default!;
    public string Author { get; private set; } = default!;
    public string? Publisher { get; private set; }
    public int? PublishedYear { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<BookCopy> Copies => _copies.AsReadOnly();

    private Book() { } // EF Core用

    /// <summary>
    /// 図書タイトルを作成
    /// </summary>
    public static Book Create(
        BookId id,
        string title,
        string author,
        string? publisher = null,
        int? publishedYear = null)
    {
        return new Book
        {
            Id = id,
            Title = title,
            Author = author,
            Publisher = publisher,
            PublishedYear = publishedYear,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ================================================================
    // Boundaryメソッド（操作可否判定）
    // ================================================================

    /// <summary>
    /// 更新可否を判定
    /// </summary>
    public BoundaryDecision CanUpdate()
    {
        if (!IsActive)
            return BoundaryDecision.Deny("非アクティブな図書は更新できません");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 非アクティブ化の可否を判定
    /// </summary>
    public BoundaryDecision CanDeactivate()
    {
        if (!IsActive)
            return BoundaryDecision.Deny("既に非アクティブです");

        // 貸出中のコピーがあれば非アクティブ化不可
        if (_copies.Any(c => c.Status == CopyStatus.OnLoan))
            return BoundaryDecision.Deny("貸出中のコピーがあるため非アクティブ化できません");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 予約可否を判定
    /// 「利用可能なコピーが1冊もない場合のみ予約可能」というルール
    /// </summary>
    public BoundaryDecision CanReserve()
    {
        if (!IsActive)
            return BoundaryDecision.Deny("非アクティブな図書は予約できません");

        // 利用可能なコピーがあれば予約不可（直接借りられる）
        if (_copies.Any(c => c.Status == CopyStatus.Available))
            return BoundaryDecision.Deny("貸出可能なコピーがあるため予約できません。直接貸出してください");

        return BoundaryDecision.Allow();
    }

    // ================================================================
    // 状態変更メソッド
    // ================================================================

    /// <summary>
    /// 図書情報を更新
    /// </summary>
    public void Update(string title, string author, string? publisher, int? publishedYear)
    {
        var decision = CanUpdate();
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Reason);

        Title = title;
        Author = author;
        Publisher = publisher;
        PublishedYear = publishedYear;
    }

    /// <summary>
    /// 非アクティブ化
    /// </summary>
    public void Deactivate()
    {
        var decision = CanDeactivate();
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Reason);

        IsActive = false;
    }

    /// <summary>
    /// 蔵書コピーを追加
    /// </summary>
    public BookCopy AddCopy()
    {
        var copyNumber = _copies.Count + 1;
        var copy = BookCopy.Create(CopyId.New(), Id, copyNumber);
        _copies.Add(copy);
        return copy;
    }

    /// <summary>
    /// 利用可能なコピーを取得
    /// </summary>
    public BookCopy? GetAvailableCopy()
    {
        return _copies.FirstOrDefault(c => c.Status == CopyStatus.Available);
    }
}
