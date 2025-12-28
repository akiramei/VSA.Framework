using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using Domain.Library.Books;
using Domain.Library.Members;
using Domain.Library.Loans;
using Domain.Library.Reservations;

namespace Application.Features.LendCopy;

/// <summary>
/// 蔵書コピー貸出Handler
///
/// 【パターン: 複数集約をまたがる操作】
///
/// このHandlerは以下を実行:
/// 1. BookCopy, Member, (Reservation) を取得
/// 2. 各エンティティの CanXxx() で操作可否を判定
/// 3. Loan を作成
/// 4. BookCopy のステータスを変更
/// 5. 予約があれば完了扱いに
///
/// 注意: SaveChangesAsync は呼ばない（TransactionBehaviorが自動実行）
/// </summary>
public sealed class LendCopyHandler : IRequestHandler<LendCopyCommand, Result<LoanId>>
{
    private readonly IBookCopyRepository _copyRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<LendCopyHandler> _logger;

    public LendCopyHandler(
        IBookCopyRepository copyRepository,
        IMemberRepository memberRepository,
        ILoanRepository loanRepository,
        IReservationRepository reservationRepository,
        ILogger<LendCopyHandler> logger)
    {
        _copyRepository = copyRepository;
        _memberRepository = memberRepository;
        _loanRepository = loanRepository;
        _reservationRepository = reservationRepository;
        _logger = logger;
    }

    public async Task<Result<LoanId>> Handle(
        LendCopyCommand command,
        CancellationToken cancellationToken)
    {
        // 1. BookCopy を取得
        var copy = await _copyRepository.GetByIdAsync(command.CopyId, cancellationToken);
        if (copy is null)
            return Result.Fail<LoanId>("蔵書コピーが見つかりません");

        // 2. Member を取得
        var member = await _memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Result.Fail<LoanId>("利用者が見つかりません");

        // 3. Member の貸出可否を判定
        var memberDecision = member.CanBorrow();
        if (!memberDecision.IsAllowed)
            return Result.Fail<LoanId>(memberDecision.Reason!);

        // 4. 予約の確認（Ready状態の予約者がいるか）
        MemberId? readyReservationMemberId = null;
        Reservation? reservation = null;

        if (command.ReservationId.HasValue)
        {
            reservation = await _reservationRepository.GetByIdAsync(
                command.ReservationId.Value, cancellationToken);

            if (reservation is null)
                return Result.Fail<LoanId>("予約が見つかりません");

            var reservationDecision = reservation.CanCheckout();
            if (!reservationDecision.IsAllowed)
                return Result.Fail<LoanId>(reservationDecision.Reason!);

            readyReservationMemberId = reservation.MemberId;
        }
        else
        {
            // 予約からの貸出でない場合、Ready状態の予約があるか確認
            var readyReservation = await _reservationRepository
                .GetReadyReservationByBookIdAsync(copy.BookId, cancellationToken);

            if (readyReservation is not null)
            {
                readyReservationMemberId = readyReservation.MemberId;
            }
        }

        // 5. BookCopy の貸出可否を判定
        var copyDecision = copy.CanLend(readyReservationMemberId, command.MemberId);
        if (!copyDecision.IsAllowed)
            return Result.Fail<LoanId>(copyDecision.Reason!);

        // 6. Loan を作成
        var loanId = LoanId.New();
        var loan = Loan.Create(loanId, copy.Id, copy.BookId, command.MemberId);
        await _loanRepository.AddAsync(loan, cancellationToken);

        // 7. BookCopy のステータスを変更
        copy.MarkAsOnLoan();

        // 8. 予約があれば完了扱いに
        if (reservation is not null)
        {
            reservation.Complete();
        }

        _logger.LogInformation(
            "貸出完了: LoanId={LoanId}, CopyId={CopyId}, MemberId={MemberId}",
            loanId.Value, command.CopyId.Value, command.MemberId.Value);

        // SaveChangesAsync は呼ばない！TransactionBehavior が自動実行する
        return Result.Success(loanId);
    }
}

// ================================================================
// Repository インターフェース（本来は別ファイルに配置）
// ================================================================

public interface IBookCopyRepository
{
    Task<BookCopy?> GetByIdAsync(CopyId id, CancellationToken ct);
}

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(MemberId id, CancellationToken ct);
}

public interface ILoanRepository
{
    Task AddAsync(Loan loan, CancellationToken ct);
    Task<Loan?> GetByIdAsync(LoanId id, CancellationToken ct);
}

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(ReservationId id, CancellationToken ct);
    Task<Reservation?> GetReadyReservationByBookIdAsync(BookId bookId, CancellationToken ct);
}
