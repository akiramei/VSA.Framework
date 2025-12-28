using VSA.Application;
using VSA.Application.Interfaces;

namespace Application.Features.GetTasks;

/// <summary>
/// タスク一覧取得Query
/// </summary>
public sealed record GetTasksQuery(
    string? TitleFilter = null,
    string? StatusFilter = null,
    int Page = 1,
    int PageSize = 20
) : IQuery<Result<PagedResult<TaskDto>>>;
