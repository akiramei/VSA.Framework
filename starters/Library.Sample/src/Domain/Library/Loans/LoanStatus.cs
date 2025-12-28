namespace Domain.Library.Loans;

/// <summary>
/// 貸出のステータス
/// </summary>
public enum LoanStatus
{
    /// <summary>貸出中</summary>
    OnLoan,

    /// <summary>返却済み</summary>
    Returned,

    /// <summary>延滞中</summary>
    Overdue
}
