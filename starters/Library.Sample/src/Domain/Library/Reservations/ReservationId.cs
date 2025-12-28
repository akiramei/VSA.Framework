using VSA.Kernel;

namespace Domain.Library.Reservations;

/// <summary>
/// 予約の型付きID
/// </summary>
public readonly record struct ReservationId(Guid Value) : ITypedId
{
    public static ReservationId New() => new(Guid.NewGuid());
    public static ReservationId From(Guid value) => new(value);
}
