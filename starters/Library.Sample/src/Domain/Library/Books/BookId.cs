using VSA.Kernel;

namespace Domain.Library.Books;

/// <summary>
/// 図書タイトルの型付きID
/// </summary>
public readonly record struct BookId(Guid Value) : ITypedId
{
    public static BookId New() => new(Guid.NewGuid());
    public static BookId From(Guid value) => new(value);
}
