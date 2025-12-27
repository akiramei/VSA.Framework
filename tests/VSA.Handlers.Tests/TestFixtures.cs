using System.Linq.Expressions;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Handlers.Abstractions;
using VSA.Kernel;

namespace VSA.Handlers.Tests;

// Test Entity
public class TestProduct : AggregateRoot<Guid>, ISoftDeletable
{
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private TestProduct() { }

    public TestProduct(Guid id, string name, decimal price) : base(id)
    {
        Name = name;
        Price = price;
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void UpdatePrice(decimal price)
    {
        Price = price;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}

// Test Commands
public record CreateProductCommand(string Name, decimal Price) : ICommand<Result<Guid>>;

public record UpdateProductCommand(Guid Id, string Name, decimal Price)
    : ICommand<Result>, IEntityCommand<Guid>;

public record DeleteProductCommand(Guid Id) : ICommand<Result>, IEntityCommand<Guid>;

// Test Queries
public record GetProductByIdQuery(Guid Id) : IQuery<Result<TestProduct>>, IGetByIdQuery<Guid>;

// In-Memory Repository for testing
public class InMemoryRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
{
    private readonly List<TEntity> _entities = [];

    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default)
    {
        var entity = _entities.FirstOrDefault(e => e.Id!.Equals(id));
        return Task.FromResult(entity);
    }

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        var compiled = predicate.Compile();
        var entity = _entities.FirstOrDefault(compiled);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        IEnumerable<TEntity> query = _entities;
        if (predicate != null)
        {
            query = query.Where(predicate.Compile());
        }
        return Task.FromResult<IReadOnlyList<TEntity>>(query.ToList());
    }

    public Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedListAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        IEnumerable<TEntity> query = _entities;
        if (predicate != null)
        {
            query = query.Where(predicate.Compile());
        }

        var totalCount = query.Count();
        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<(IReadOnlyList<TEntity> Items, int TotalCount)>((items, totalCount));
    }

    public Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        // In-memory doesn't need to do anything for updates
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        var exists = _entities.Any(predicate.Compile());
        return Task.FromResult(exists);
    }

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        IEnumerable<TEntity> query = _entities;
        if (predicate != null)
        {
            query = query.Where(predicate.Compile());
        }
        return Task.FromResult(query.Count());
    }

    // Helper for seeding test data
    public void Seed(params TEntity[] entities)
    {
        _entities.AddRange(entities);
    }
}
