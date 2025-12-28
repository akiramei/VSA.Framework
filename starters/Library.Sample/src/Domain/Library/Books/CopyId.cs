using VSA.Kernel;

namespace Domain.Library.Books;

/// <summary>
/// 蔵書コピーの型付きID
/// </summary>
public readonly record struct CopyId(Guid Value) : ITypedId
{
    public static CopyId New() => new(Guid.NewGuid());
    public static CopyId From(Guid value) => new(value);
}
