using VSA.Application;
using VSA.Application.Interfaces;
using Domain.TaskManagement;

namespace Application.Features.CreateTask;

/// <summary>
/// タスク作成Command
/// </summary>
public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    DateTime? DueDate
) : ICommand<Result<TaskId>>;
