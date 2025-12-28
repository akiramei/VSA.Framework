using VSA.Kernel;

namespace Domain.TaskManagement;

/// <summary>
/// タスク集約ルート
/// </summary>
public sealed class TaskItem : AggregateRoot<TaskId>
{
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private TaskItem() { } // EF Core用

    /// <summary>
    /// タスクを作成
    /// </summary>
    public static TaskItem Create(TaskId id, string title, string? description = null, DateTime? dueDate = null)
    {
        return new TaskItem
        {
            Id = id,
            Title = title,
            Description = description,
            Status = TaskStatus.Todo,
            DueDate = dueDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ================================================================
    // Boundaryメソッド（操作可否判定）
    // ================================================================

    /// <summary>
    /// 更新可否を判定
    /// </summary>
    public BoundaryDecision CanUpdate()
    {
        if (IsDeleted)
            return BoundaryDecision.Deny("削除されたタスクは更新できません");

        if (Status == TaskStatus.Done)
            return BoundaryDecision.Deny("完了したタスクは更新できません");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 削除可否を判定
    /// </summary>
    public BoundaryDecision CanDelete()
    {
        if (IsDeleted)
            return BoundaryDecision.Deny("既に削除されています");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 完了可否を判定
    /// </summary>
    public BoundaryDecision CanComplete()
    {
        if (IsDeleted)
            return BoundaryDecision.Deny("削除されたタスクは完了できません");

        return Status switch
        {
            TaskStatus.Done => BoundaryDecision.Deny("既に完了しています"),
            TaskStatus.Todo => BoundaryDecision.Deny("進行中にしてから完了してください"),
            TaskStatus.InProgress => BoundaryDecision.Allow(),
            _ => BoundaryDecision.Deny("この状態では完了できません")
        };
    }

    /// <summary>
    /// 開始可否を判定
    /// </summary>
    public BoundaryDecision CanStart()
    {
        if (IsDeleted)
            return BoundaryDecision.Deny("削除されたタスクは開始できません");

        return Status switch
        {
            TaskStatus.Todo => BoundaryDecision.Allow(),
            TaskStatus.InProgress => BoundaryDecision.Deny("既に進行中です"),
            TaskStatus.Done => BoundaryDecision.Deny("完了したタスクは開始できません"),
            _ => BoundaryDecision.Deny("この状態では開始できません")
        };
    }

    // ================================================================
    // 状態変更メソッド
    // ================================================================

    /// <summary>
    /// タスクを更新
    /// </summary>
    public void Update(string title, string? description, DateTime? dueDate)
    {
        Title = title;
        Description = description;
        DueDate = dueDate;
    }

    /// <summary>
    /// タスクを開始（Todo → InProgress）
    /// </summary>
    public void Start()
    {
        var decision = CanStart();
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Reason);

        Status = TaskStatus.InProgress;
    }

    /// <summary>
    /// タスクを完了（InProgress → Done）
    /// </summary>
    public void Complete()
    {
        var decision = CanComplete();
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Reason);

        Status = TaskStatus.Done;
    }

    /// <summary>
    /// タスクを削除（論理削除）
    /// </summary>
    public void Delete()
    {
        var decision = CanDelete();
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Reason);

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
