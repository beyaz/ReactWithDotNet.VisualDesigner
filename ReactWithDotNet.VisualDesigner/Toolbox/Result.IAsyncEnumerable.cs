namespace Toolbox;

partial class ResultExtensions
{
    public static async Task<Result<IReadOnlyList<T>>> AsResult<T>(this IAsyncEnumerable<Result<T>> enumerable)
    {
        List<T> items = [];

        await foreach (var result in enumerable)
        {
            items.Add(result.Value);

            if (result.HasError)
            {
                return new()
                {
                    Error = result.Error
                };
            }
        }

        return items;
    }

    public static async IAsyncEnumerable<Result<B>> Select<A, B>(
        this IAsyncEnumerable<Result<A>> source,
        Func<A, B> selector
    )
    {
        await foreach (var result in source)
        {
            if (result.HasError)
            {
                yield return result.Error;
                yield break;
            }

            yield return selector(result.Value);
        }
    }

    public static async IAsyncEnumerable<Result<B>> Select<A, B>(
        this IAsyncEnumerable<Result<A>> source,
        Func<A, Task<Result<B>>> selector
    )
    {
        await foreach (var result in source)
        {
            if (result.HasError)
            {
                yield return result.Error;
                yield break;
            }

            yield return await selector(result.Value);
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>(
        this Result<IEnumerable<A>> source,
        Func<A, Task<Result<B>>> bindAsync,
        Func<A, B, Result<C>> selector
    )
    {
        if (source.HasError)
        {
            yield return source.Error;
            yield break;
        }

        foreach (var a in source.Value)
        {
            var b = await bindAsync(a);
            if (b.HasError)
            {
                yield return b.Error;
                yield break;
            }

            var c = selector(a, b.Value);
            if (c.HasError)
            {
                yield return c.Error;
                yield break;
            }

            yield return c.Value;
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>(
        this Result<IEnumerable<A>> source,
        Func<A, Task<Result<B>>> bindAsync,
        Func<A, B, C> selector
    )
    {
        if (source.HasError)
        {
            yield return source.Error;
            yield break;
        }

        foreach (var a in source.Value)
        {
            var b = await bindAsync(a);
            if (b.HasError)
            {
                yield return b.Error;
                yield break;
            }

            yield return selector(a, b.Value);
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>(
        this Task<Result<A>> source,
        Func<A, IEnumerable<B>> bind,
        Func<A, B, C> resultSelector
    )
    {
        var a = await source;

        if (a.HasError)
        {
            yield return a.Error;
            yield break;
        }

        var enumerable = bind(a.Value);
        foreach (var b in enumerable)
        {
            yield return resultSelector(a.Value, b);
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>(
        this IAsyncEnumerable<Result<A>> source,
        Func<A, Task<Result<B>>> bind,
        Func<A, B, Result<C>> resultSelector
    )
    {
        await foreach (var a in source)
        {
            if (a.HasError)
            {
                yield return a.Error;
                yield break;
            }

            var b = await bind(a.Value);
            if (b.HasError)
            {
                yield return b.Error;
                yield break;
            }

            yield return resultSelector(a.Value, b.Value);
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>(
        this IAsyncEnumerable<Result<A>> source,
        Func<A, Result<B>> bind,
        Func<A, B, C> resultSelector
    )
    {
        await foreach (var a in source)
        {
            if (a.HasError)
            {
                yield return a.Error;
                yield break;
            }

            var b = bind(a.Value);
            if (b.HasError)
            {
                yield return b.Error;
                yield break;
            }

            yield return resultSelector(a.Value, b.Value);
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>(
        this IAsyncEnumerable<Result<A>> source,
        Func<A, Task<Result<B>>> bind,
        Func<A, B, Task<Result<C>>> resultSelector
    )
    {
        await foreach (var a in source)
        {
            if (a.HasError)
            {
                yield return a.Error;
                yield break;
            }

            var b = await bind(a.Value);
            if (b.HasError)
            {
                yield return b.Error;
                yield break;
            }

            yield return await resultSelector(a.Value, b.Value);
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>(
        this IEnumerable<A> source,
        Func<A, Task<Result<B>>> bind,
        Func<A, B, C> resultSelector
    )
    {
        if (source == null)
        {
            yield return Result.Error<C>(new ArgumentNullException(nameof(source)));
            yield break;
        }

        if (bind == null)
        {
            yield return Result.Error<C>(new ArgumentNullException(nameof(bind)));
            yield break;
        }

        if (resultSelector == null)
        {
            yield return Result.Error<C>(new ArgumentNullException(nameof(resultSelector)));
            yield break;
        }

        foreach (var a in source)
        {
            var b = await bind(a);
            if (b.HasError)
            {
                yield return b.Error;
                yield break;
            }

            yield return resultSelector(a, b.Value);
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>(
        this IAsyncEnumerable<Result<A>> source,
        Func<A, IEnumerable<B>> bind,
        Func<A, B, Result<C>> resultSelector
    )
    {
        await foreach (var a in source)
        {
            if (a.HasError)
            {
                yield return a.Error;
                yield break;
            }

            var enumerableB = bind(a.Value);
            foreach (var b in enumerableB)
            {
                yield return resultSelector(a.Value, b);
            }
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>(
        this IAsyncEnumerable<Result<A>> source,
        Func<A, IEnumerable<B>> bind,
        Func<A, B, C> resultSelector
    )
    {
        await foreach (var a in source)
        {
            if (a.HasError)
            {
                yield return a.Error;
                yield break;
            }

            var enumerableB = bind(a.Value);
            foreach (var b in enumerableB)
            {
                yield return resultSelector(a.Value, b);
            }
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>(
        this IAsyncEnumerable<Result<A>> source,
        Func<A, Task<Result<B>>> bind,
        Func<A, B, C> resultSelector
    )
    {
        await foreach (var a in source)
        {
            if (a.HasError)
            {
                yield return a.Error;
                yield break;
            }

            var resultB = await bind(a.Value);
            if (resultB.HasError)
            {
                yield return resultB.Error;
                yield break;
            }

            yield return resultSelector(a.Value, resultB.Value);
        }
    }

    public static async IAsyncEnumerable<Result<A>> Where<A>(
        this IAsyncEnumerable<Result<A>> source,
        Func<A, bool> predicate)
    {
        if (source == null)
        {
            yield return Result.Error<A>(new ArgumentNullException(nameof(source)));
            yield break;
        }

        if (predicate == null)
        {
            yield return Result.Error<A>(new ArgumentNullException(nameof(predicate)));
            yield break;
        }

        await foreach (var result in source)
        {
            if (result.HasError)
            {
                yield return new Result<A> { Error = result.Error };
                yield break;
            }

            if (predicate(result.Value))
            {
                yield return result;
            }
        }
    }
}