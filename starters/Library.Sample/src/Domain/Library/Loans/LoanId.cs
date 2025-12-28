using VSA.Kernel;

namespace Domain.Library.Loans;

/// <summary>
/// 貸出の型付きID
/// </summary>
public readonly record struct LoanId(Guid Value) : ITypedId
{
    public static LoanId New() => new(Guid.NewGuid());
    public static LoanId From(Guid value) => new(value);
}
