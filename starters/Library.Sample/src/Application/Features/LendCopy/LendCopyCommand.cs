using VSA.Application;
using VSA.Application.Interfaces;
using Domain.Library.Books;
using Domain.Library.Members;
using Domain.Library.Loans;

namespace Application.Features.LendCopy;

/// <summary>
/// 蔵書コピー貸出Command
///
/// 【ビジネスルール】
/// - CopyId が Available であること
/// - Member が Active であること
/// - 予約がある場合、Ready状態の予約者のみ貸出可能
/// </summary>
public sealed record LendCopyCommand(
    CopyId CopyId,
    MemberId MemberId,
    ReservationId? ReservationId = null  // 予約からの貸出の場合
) : ICommand<Result<LoanId>>;
