using VSA.Kernel;
using Domain.Library.Members;

namespace Domain.Library.Books;

/// <summary>
/// 蔵書コピー
///
/// 実物としての1冊を表す。
/// Book と 1:N の関係。
/// </summary>
public sealed class BookCopy : Entity<CopyId>
{
    public BookId BookId { get; private set; }
    public int CopyNumber { get; private set; }
    public CopyStatus Status { get; private set; }
    public string? Location { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private BookCopy() { } // EF Core用

    /// <summary>
    /// 蔵書コピーを作成
    /// </summary>
    internal static BookCopy Create(CopyId id, BookId bookId, int copyNumber)
    {
        return new BookCopy
        {
            Id = id,
            BookId = bookId,
            CopyNumber = copyNumber,
            Status = CopyStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ================================================================
    // Boundaryメソッド（操作可否判定）
    // ================================================================

    /// <summary>
    /// 貸出可否を判定
    /// </summary>
    /// <param name="readyReservationMemberId">Ready状態の予約者（いれば）</param>
    /// <param name="borrowingMemberId">借りようとしている会員</param>
    public BoundaryDecision CanLend(MemberId? readyReservationMemberId, MemberId borrowingMemberId)
    {
        // 基本条件: 貸出可能な状態か
        if (Status == CopyStatus.Inactive)
            return BoundaryDecision.Deny("このコピーは利用不可です");

        if (Status == CopyStatus.OnLoan)
            return BoundaryDecision.Deny("このコピーは既に貸出中です");

        // 予約で確保されている場合
        if (Status == CopyStatus.Reserved)
        {
            // Ready状態の予約者のみ貸出可能
            if (!readyReservationMemberId.HasValue)
                return BoundaryDecision.Deny("このコピーは予約で確保されています");

            if (readyReservationMemberId.Value != borrowingMemberId)
                return BoundaryDecision.Deny("予約者に優先権があります");
        }

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 返却可否を判定
    /// </summary>
    public BoundaryDecision CanReturn()
    {
        if (Status != CopyStatus.OnLoan)
            return BoundaryDecision.Deny("貸出中ではありません");

        return BoundaryDecision.Allow();
    }

    // ================================================================
    // 状態変更メソッド
    // ================================================================

    /// <summary>
    /// 貸出中に変更
    /// </summary>
    public void MarkAsOnLoan()
    {
        Status = CopyStatus.OnLoan;
    }

    /// <summary>
    /// 利用可能に変更
    /// </summary>
    public void MarkAsAvailable()
    {
        Status = CopyStatus.Available;
    }

    /// <summary>
    /// 予約で確保済みに変更
    /// </summary>
    public void MarkAsReserved()
    {
        Status = CopyStatus.Reserved;
    }

    /// <summary>
    /// 利用不可に変更
    /// </summary>
    public void MarkAsInactive()
    {
        if (Status == CopyStatus.OnLoan)
            throw new InvalidOperationException("貸出中のコピーは利用不可にできません");

        Status = CopyStatus.Inactive;
    }

    /// <summary>
    /// 場所を設定
    /// </summary>
    public void SetLocation(string? location)
    {
        Location = location;
    }

    /// <summary>
    /// メモを設定
    /// </summary>
    public void SetNotes(string? notes)
    {
        Notes = notes;
    }
}
