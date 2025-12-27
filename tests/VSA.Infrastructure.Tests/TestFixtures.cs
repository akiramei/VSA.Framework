using MediatR;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Infrastructure.Abstractions;

namespace VSA.Infrastructure.Tests;

// Public test types to avoid Moq/Castle.DynamicProxy strong-naming issues
public record TestCommand(string Name) : ICommand<Result<string>>;
public record TestQuery(string Name) : IQuery<Result<string>>, IRequest<Result<string>>;
public record TestNonResultCommand(string Name) : IRequest<string>;
public record TestCacheableQuery(string Key) : IQuery<Result<string>>, ICacheableQuery
{
    public string GetCacheKey() => $"test:{Key}";
    public int CacheDurationMinutes => 5;
}
