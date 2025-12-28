using VSA.Kernel;

namespace Domain.Library.Members;

/// <summary>
/// 利用者の型付きID
/// </summary>
public readonly record struct MemberId(Guid Value) : ITypedId
{
    public static MemberId New() => new(Guid.NewGuid());
    public static MemberId From(Guid value) => new(value);
}
