using VSA.Kernel;

namespace Domain.TaskManagement;

/// <summary>
/// タスクの型付きID
/// </summary>
public readonly record struct TaskId(Guid Value) : ITypedId
{
    /// <summary>
    /// 新しいIDを生成
    /// </summary>
    public static TaskId New() => new(Guid.NewGuid());

    /// <summary>
    /// 既存のGuidからIDを生成
    /// </summary>
    public static TaskId From(Guid value) => new(value);
}
