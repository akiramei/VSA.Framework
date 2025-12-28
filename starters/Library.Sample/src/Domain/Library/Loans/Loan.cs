using VSA.Kernel;
using Domain.Library.Books;
using Domain.Library.Members;

namespace Domain.Library.Loans;

/// <summary>
/// 貸出（集約ルート）
///
/// 蔵書コピーを借りた記録を管理する。
/// </summary>
public sealed class Loan : AggregateRoot<LoanId>
{
    /// <summary>貸出期間（日数）</summary>
    public const int LoanPeriodDays = 14;

    public CopyId CopyId { get; private set; }
    public BookId BookId { get; private set; }
    public MemberId MemberId { get; private set; }
    public DateTime LoanDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? ReturnDate { get; private set; }
    public LoanStatus Status { get; private set; }

    private Loan() { } // EF Core用

    /// <summary>
    /// 貸出を作成
    /// </summary>
    public static Loan Create(LoanId id, CopyId copyId, BookId bookId, MemberId memberId)
    {
        var loanDate = DateTime.UtcNow.Date;
        return new Loan
        {
            Id = id,
            CopyId = copyId,
            BookId = bookId,
            MemberId = memberId,
            LoanDate = loanDate,
            DueDate = loanDate.AddDays(LoanPeriodDays),
            Status = LoanStatus.OnLoan
        };
    }

    // ================================================================
    // Boundaryメソッド（操作可否判定）
    // ================================================================

    /// <summary>
    /// 返却可否を判定
    /// </summary>
    public BoundaryDecision CanReturn()
    {
        if (Status == LoanStatus.Returned)
            return BoundaryDecision.Deny("既に返却済みです");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 延滞かどうかを判定
    /// </summary>
    public bool IsOverdue()
    {
        return Status != LoanStatus.Returned
               && DueDate < DateTime.UtcNow.Date
               && ReturnDate == null;
    }

    // ================================================================
    // 状態変更メソッド
    // ================================================================

    /// <summary>
    /// 返却処理
    /// </summary>
    public void Return()
    {
        var decision = CanReturn();
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Reason);

        ReturnDate = DateTime.UtcNow;
        Status = LoanStatus.Returned;
    }

    /// <summary>
    /// 延滞に更新（バッチ処理用）
    /// </summary>
    public void MarkAsOverdue()
    {
        if (IsOverdue() && Status == LoanStatus.OnLoan)
        {
            Status = LoanStatus.Overdue;
        }
    }
}
