using VSA.Kernel;
using Domain.Library.Books;
using Domain.Library.Members;

namespace Domain.Library.Reservations;

/// <summary>
/// 予約（集約ルート）
///
/// 貸出中の本に対する予約を管理する。
/// 予約はBook単位（タイトル単位）で行い、
/// 返却されたコピーがどれでも貸出可能になる。
/// </summary>
public sealed class Reservation : AggregateRoot<ReservationId>
{
    public BookId BookId { get; private set; }
    public MemberId MemberId { get; private set; }
    public DateTime ReservedAt { get; private set; }
    public ReservationStatus Status { get; private set; }
    public int Position { get; private set; }

    private Reservation() { } // EF Core用

    /// <summary>
    /// 予約を作成
    /// </summary>
    public static Reservation Create(ReservationId id, BookId bookId, MemberId memberId, int position)
    {
        return new Reservation
        {
            Id = id,
            BookId = bookId,
            MemberId = memberId,
            ReservedAt = DateTime.UtcNow,
            Status = ReservationStatus.Waiting,
            Position = position
        };
    }

    // ================================================================
    // Boundaryメソッド（操作可否判定）
    // ================================================================

    /// <summary>
    /// キャンセル可否を判定
    /// </summary>
    public BoundaryDecision CanCancel()
    {
        if (Status == ReservationStatus.Cancelled)
            return BoundaryDecision.Deny("既にキャンセル済みです");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 貸出可否を判定（Ready状態の予約者が来館した場合）
    /// </summary>
    public BoundaryDecision CanCheckout()
    {
        if (Status != ReservationStatus.Ready)
            return BoundaryDecision.Deny("まだ借りられる状態ではありません");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// Ready状態にできるか判定
    /// </summary>
    public BoundaryDecision CanPromoteToReady()
    {
        if (Status != ReservationStatus.Waiting)
            return BoundaryDecision.Deny("待機中の予約ではありません");

        return BoundaryDecision.Allow();
    }

    // ================================================================
    // 状態変更メソッド
    // ================================================================

    /// <summary>
    /// キャンセル
    /// </summary>
    public void Cancel()
    {
        var decision = CanCancel();
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Reason);

        Status = ReservationStatus.Cancelled;
    }

    /// <summary>
    /// Ready状態に変更（返却時に先頭の予約を貸出可能にする）
    /// </summary>
    public void PromoteToReady()
    {
        var decision = CanPromoteToReady();
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Reason);

        Status = ReservationStatus.Ready;
    }

    /// <summary>
    /// 貸出完了でキャンセル扱いに
    /// </summary>
    public void Complete()
    {
        var decision = CanCheckout();
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Reason);

        Status = ReservationStatus.Cancelled; // 完了 = キャンセル扱い
    }

    /// <summary>
    /// 順番を繰り上げ
    /// </summary>
    public void PromotePosition()
    {
        if (Position > 1)
        {
            Position--;
        }
    }
}
