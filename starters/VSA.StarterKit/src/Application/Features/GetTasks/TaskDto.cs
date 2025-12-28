namespace Application.Features.GetTasks;

/// <summary>
/// タスク一覧用DTO
/// </summary>
public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateTime? DueDate,
    DateTime CreatedAt
);
