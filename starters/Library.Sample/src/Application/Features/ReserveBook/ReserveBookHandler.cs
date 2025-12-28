using MediatR;
using Microsoft.Extensions.Logging;
using VSA.Application;
using Domain.Library.Books;
using Domain.Library.Members;
using Domain.Library.Reservations;

namespace Application.Features.ReserveBook;

/// <summary>
/// 図書予約Handler
///
/// 【パターン: 予約キュー管理】
///
/// このHandlerは以下を実行:
/// 1. Book を取得して予約可能か判定（利用可能なコピーがないこと）
/// 2. Member を取得して予約可能か判定
/// 3. 現在の予約数を取得して Position を決定
/// 4. Reservation を作成
///
/// 注意: SaveChangesAsync は呼ばない（TransactionBehaviorが自動実行）
/// </summary>
public sealed class ReserveBookHandler : IRequestHandler<ReserveBookCommand, Result<ReservationId>>
{
    private readonly IBookRepository _bookRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<ReserveBookHandler> _logger;

    public ReserveBookHandler(
        IBookRepository bookRepository,
        IMemberRepository memberRepository,
        IReservationRepository reservationRepository,
        ILogger<ReserveBookHandler> logger)
    {
        _bookRepository = bookRepository;
        _memberRepository = memberRepository;
        _reservationRepository = reservationRepository;
        _logger = logger;
    }

    public async Task<Result<ReservationId>> Handle(
        ReserveBookCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Book を取得（コピー情報も含む）
        var book = await _bookRepository.GetByIdWithCopiesAsync(command.BookId, cancellationToken);
        if (book is null)
            return Result.Fail<ReservationId>("図書が見つかりません");

        // 2. Book の予約可否を判定
        var bookDecision = book.CanReserve();
        if (!bookDecision.IsAllowed)
            return Result.Fail<ReservationId>(bookDecision.Reason!);

        // 3. Member を取得
        var member = await _memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Result.Fail<ReservationId>("利用者が見つかりません");

        // 4. Member の予約可否を判定
        var memberDecision = member.CanReserve();
        if (!memberDecision.IsAllowed)
            return Result.Fail<ReservationId>(memberDecision.Reason!);

        // 5. 現在の予約数を取得して Position を決定
        var currentCount = await _reservationRepository
            .GetActiveReservationCountAsync(command.BookId, cancellationToken);
        var position = currentCount + 1;

        // 6. Reservation を作成
        var reservationId = ReservationId.New();
        var reservation = Reservation.Create(
            reservationId,
            command.BookId,
            command.MemberId,
            position);

        await _reservationRepository.AddAsync(reservation, cancellationToken);

        _logger.LogInformation(
            "予約完了: ReservationId={ReservationId}, BookId={BookId}, MemberId={MemberId}, Position={Position}",
            reservationId.Value, command.BookId.Value, command.MemberId.Value, position);

        // SaveChangesAsync は呼ばない！TransactionBehavior が自動実行する
        return Result.Success(reservationId);
    }
}

// ================================================================
// Repository インターフェース
// ================================================================

public interface IBookRepository
{
    Task<Book?> GetByIdWithCopiesAsync(BookId id, CancellationToken ct);
}

public interface IReservationRepository
{
    Task AddAsync(Reservation reservation, CancellationToken ct);
    Task<int> GetActiveReservationCountAsync(BookId bookId, CancellationToken ct);
}
