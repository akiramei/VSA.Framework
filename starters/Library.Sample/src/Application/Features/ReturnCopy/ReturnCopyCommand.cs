using VSA.Application;
using VSA.Application.Interfaces;
using Domain.Library.Loans;

namespace Application.Features.ReturnCopy;

/// <summary>
/// 返却Command
///
/// 【ビジネスルール】
/// - Loan が OnLoan または Overdue であること
/// - 返却後、予約があれば先頭をReady状態にする
/// </summary>
public sealed record ReturnCopyCommand(
    LoanId LoanId
) : ICommand<Result<Unit>>;
