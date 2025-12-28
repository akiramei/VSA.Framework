using Microsoft.EntityFrameworkCore;
using MediatR;
using VSA.Application;
using Domain.TaskManagement;

namespace Application.Features.GetTasks;

/// <summary>
/// タスク一覧取得Handler
/// </summary>
public sealed class GetTasksHandler
    : IRequestHandler<GetTasksQuery, Result<PagedResult<TaskDto>>>
{
    private readonly ApplicationDbContext _context;

    public GetTasksHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<TaskDto>>> Handle(
        GetTasksQuery query,
        CancellationToken cancellationToken)
    {
        // AsNoTracking: 読み取り専用クエリでは必須
        var queryable = _context.Tasks
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        // フィルタリング
        if (!string.IsNullOrEmpty(query.TitleFilter))
        {
            queryable = queryable.Where(x => x.Title.Contains(query.TitleFilter));
        }

        if (!string.IsNullOrEmpty(query.StatusFilter))
        {
            if (Enum.TryParse<TaskStatus>(query.StatusFilter, out var status))
            {
                queryable = queryable.Where(x => x.Status == status);
            }
        }

        // 件数取得
        var totalCount = await queryable.CountAsync(cancellationToken);

        // ソート + ページング
        var items = await queryable
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new TaskDto(
                x.Id.Value,
                x.Title,
                x.Description,
                x.Status.ToString(),
                x.DueDate,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        var result = new PagedResult<TaskDto>(
            items,
            totalCount,
            query.Page,
            query.PageSize);

        return Result.Success(result);
    }
}
