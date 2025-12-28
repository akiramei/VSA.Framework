namespace Domain.Library.Reservations;

/// <summary>
/// 予約のステータス
/// </summary>
public enum ReservationStatus
{
    /// <summary>順番待ち中</summary>
    Waiting,

    /// <summary>借りられる状態（返却され取り置き済み）</summary>
    Ready,

    /// <summary>キャンセル済み</summary>
    Cancelled
}
