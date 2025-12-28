using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using Domain.Library.Books;
using Domain.Library.Loans;
using Domain.Library.Reservations;

namespace Application.Features.ReturnCopy;

/// <summary>
/// 返却Handler
///
/// 【パターン: 返却 + 予約キュー管理】
///
/// このHandlerは以下を実行:
/// 1. Loan を取得して返却可能か判定
/// 2. Loan を返却済みに変更
/// 3. BookCopy を Available または Reserved に変更
/// 4. 予約があれば先頭を Ready 状態にする
///
/// 注意: SaveChangesAsync は呼ばない（TransactionBehaviorが自動実行）
/// </summary>
public sealed class ReturnCopyHandler : IRequestHandler<ReturnCopyCommand, Result<Unit>>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBookCopyRepository _copyRepository;
    private readonly IReservationQueryService _reservationQueryService;
    private readonly ILogger<ReturnCopyHandler> _logger;

    public ReturnCopyHandler(
        ILoanRepository loanRepository,
        IBookCopyRepository copyRepository,
        IReservationQueryService reservationQueryService,
        ILogger<ReturnCopyHandler> logger)
    {
        _loanRepository = loanRepository;
        _copyRepository = copyRepository;
        _reservationQueryService = reservationQueryService;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(
        ReturnCopyCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Loan を取得
        var loan = await _loanRepository.GetByIdAsync(command.LoanId, cancellationToken);
        if (loan is null)
            return Result.Fail<Unit>("貸出記録が見つかりません");

        // 2. 返却可否を判定
        var decision = loan.CanReturn();
        if (!decision.IsAllowed)
            return Result.Fail<Unit>(decision.Reason!);

        // 3. BookCopy を取得
        var copy = await _copyRepository.GetByIdAsync(loan.CopyId, cancellationToken);
        if (copy is null)
            return Result.Fail<Unit>("蔵書コピーが見つかりません");

        // 4. Loan を返却済みに変更
        loan.Return();

        // 5. 予約を確認（Book単位で先頭の Waiting を探す）
        var nextReservation = await _reservationQueryService
            .GetNextWaitingReservationAsync(loan.BookId, cancellationToken);

        if (nextReservation is not null)
        {
            // 予約があれば: コピーを Reserved に、予約を Ready に
            copy.MarkAsReserved();
            nextReservation.PromoteToReady();

            _logger.LogInformation(
                "予約者に通知: ReservationId={ReservationId}, MemberId={MemberId}",
                nextReservation.Id.Value, nextReservation.MemberId.Value);
        }
        else
        {
            // 予約がなければ: コピーを Available に
            copy.MarkAsAvailable();
        }

        _logger.LogInformation(
            "返却完了: LoanId={LoanId}, CopyId={CopyId}",
            command.LoanId.Value, loan.CopyId.Value);

        // SaveChangesAsync は呼ばない！TransactionBehavior が自動実行する
        return Result.Success(Unit.Value);
    }
}

// ================================================================
// Query Service インターフェース
// ================================================================

public interface IReservationQueryService
{
    Task<Reservation?> GetNextWaitingReservationAsync(BookId bookId, CancellationToken ct);
}
