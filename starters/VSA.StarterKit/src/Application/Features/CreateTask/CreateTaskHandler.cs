using Microsoft.Extensions.Logging;
using VSA.Application;
using VSA.Handlers.Abstractions;
using VSA.Handlers.Commands;
using Domain.TaskManagement;

namespace Application.Features.CreateTask;

/// <summary>
/// タスク作成Handler（VSA.Handlers.CreateEntityHandler を継承）
/// </summary>
public sealed class CreateTaskHandler
    : CreateEntityHandler<CreateTaskCommand, TaskItem, TaskId>
{
    public CreateTaskHandler(
        IRepository<TaskItem, TaskId> repository,
        ILogger<CreateTaskHandler> logger)
        : base(repository, logger)
    {
    }

    protected override TaskItem CreateEntity(CreateTaskCommand command)
    {
        var id = TaskId.New();
        return TaskItem.Create(id, command.Title, command.Description, command.DueDate);
    }

    // 注意: SaveChangesAsync は呼ばない！
    // TransactionBehavior が自動でSaveChangesAsync + Commit する
}
