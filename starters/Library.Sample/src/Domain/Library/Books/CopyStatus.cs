namespace Domain.Library.Books;

/// <summary>
/// 蔵書コピーのステータス
/// </summary>
public enum CopyStatus
{
    /// <summary>貸出可能</summary>
    Available,

    /// <summary>貸出中</summary>
    OnLoan,

    /// <summary>予約で確保済み</summary>
    Reserved,

    /// <summary>破損・廃棄などで利用不可</summary>
    Inactive
}
