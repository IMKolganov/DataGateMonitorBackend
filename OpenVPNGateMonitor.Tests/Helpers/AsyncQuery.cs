using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace OpenVPNGateMonitor.Tests.Helpers;

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        => new TestAsyncEnumerable<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var replaced = AsyncToSyncMethodReducer.Reduce(expression);
        return _inner.Execute<TResult>(replaced);
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());

    public T Current => _inner.Current;
}

internal static class AsyncToSyncMethodReducer
{
    public static Expression Reduce(Expression expression)
        => new Rewriter().Visit(expression);

    private sealed class Rewriter : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Replace EF Core async methods with LINQ-to-Objects equivalents
            if (node.Method.DeclaringType?.FullName == "Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions")
            {
                var args = node.Arguments.ToList();
                // EF async methods usually have CancellationToken as last arg; drop it for LINQ equivalents
                if (args.Count > 0)
                {
                    var last = args[^1];
                    if (last.Type == typeof(System.Threading.CancellationToken))
                        args.RemoveAt(args.Count - 1);
                }
                var source = Visit(args[0]);
                var genArgs = node.Method.GetGenericArguments();
                var tSource = genArgs.Length > 0 ? genArgs[0] : null;

                Expression ReplaceWith(string name, int expectedArgCount)
                {
                    var qMethods = typeof(Queryable).GetMethods()
                        .Where(m => m.Name == name && m.IsGenericMethodDefinition)
                        .ToList();
                    foreach (var m in qMethods)
                    {
                        var pars = m.GetParameters();
                        if (pars.Length == expectedArgCount)
                        {
                            var gm = genArgs.Length == 2 ? m.MakeGenericMethod(genArgs[0], genArgs[1])
                                : genArgs.Length == 1 ? m.MakeGenericMethod(genArgs[0])
                                : m;
                            var newArgs = args.Select((a, i) => i == 0 ? source : Visit(a)).ToArray();
                            return Expression.Call(gm, newArgs);
                        }
                    }
                    return base.VisitMethodCall(node)!;
                }

                switch (node.Method.Name)
                {
                    case "FirstOrDefaultAsync":
                        // overloads: (source), (source, predicate)
                        return ReplaceWith("FirstOrDefault", args.Count);
                    case "ToListAsync":
                        return Expression.Call(
                            typeof(Enumerable), nameof(Enumerable.ToList), new[] { tSource! },
                            Expression.Call(
                                typeof(Enumerable), nameof(Enumerable.AsEnumerable), new[] { tSource! }, source));
                    case "CountAsync":
                        return ReplaceWith("Count", args.Count);
                    case "AnyAsync":
                        return ReplaceWith("Any", args.Count);
                    case "SumAsync":
                        return ReplaceWith("Sum", args.Count);
                    default:
                        break;
                }
            }

            return base.VisitMethodCall(node);
        }
    }
}
