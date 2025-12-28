namespace Domain.TaskManagement;

/// <summary>
/// タスクのステータス
/// </summary>
public enum TaskStatus
{
    /// <summary>未着手</summary>
    Todo,

    /// <summary>進行中</summary>
    InProgress,

    /// <summary>完了</summary>
    Done
}
