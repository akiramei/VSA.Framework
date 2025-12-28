using VSA.Application;
using VSA.Application.Interfaces;
using Domain.Library.Books;
using Domain.Library.Members;
using Domain.Library.Reservations;

namespace Application.Features.ReserveBook;

/// <summary>
/// 図書予約Command
///
/// 【ビジネスルール】
/// - Book の利用可能なコピーが1冊もない場合のみ予約可能
/// - Member が Active であること
/// - 予約はBook単位（タイトル単位）で行う
/// </summary>
public sealed record ReserveBookCommand(
    BookId BookId,
    MemberId MemberId
) : ICommand<Result<ReservationId>>;
