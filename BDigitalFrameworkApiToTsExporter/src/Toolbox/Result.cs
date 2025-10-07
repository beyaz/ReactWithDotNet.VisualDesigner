﻿namespace Toolbox;

public sealed class Result<TValue>
{
    // @formatter:off
    
    public TValue Value { get; init; } = default!;

    public Exception Error { get; init; } = null!;

    public bool HasError => !ReferenceEquals(Error, null);

    public static implicit operator Result<TValue>(TValue value)
    {
        return new() { Value = value};
    }

    public static implicit operator Result<TValue>(Exception error)
    {
        return new() { Error = error };
    }
    
    public static implicit operator Task<Result<TValue>>(Result<TValue> result)
    {
        return Task.FromResult(result);
    }

    // @formatter:on
}

static class Result
{
    public static Result<T> From<T>(T value)
    {
        return new() { Value = value };
    }
}

public static class ResultExtensions
{
    public static Result<T> AsResult<T>(this (T value, Exception exception) tuple)
    {
        return new() { Value = tuple.value, Error = tuple.exception };
    }

    public static Result<IEnumerable<T>> AsResult<T>(this IEnumerable<Result<T>> enumerable)
    {
        List<T> items = [];

        foreach (var result in enumerable)
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

    public static IEnumerable<Result<B>> Select<A, B>
    (
        this IEnumerable<Result<A>> source,
        Func<A, B> selector
    )
    {
        foreach (var result in source)
        {
            if (result.HasError)
            {
                yield return result.Error;
                yield break;
            }

            yield return selector(result.Value);
        }
    }

    public static async IAsyncEnumerable<Result<B>> Select<A, B>
    (
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

    public static Result<B> Select<A, B>
    (
        this Result<A> source,
        Func<A, B> selector
    )
    {
        if (source.HasError)
        {
            return source.Error;
        }

        return selector(source.Value);
    }

    public static Result<B> Select<A, B>
    (
        this Result<A> source,
        Func<A, Result<B>> selector
    )
    {
        if (source.HasError)
        {
            return source.Error;
        }

        return selector(source.Value);
    }

    public static async Task<Result<B>> Select<A, B>
    (
        this Task<Result<A>> source,
        Func<A, B> selector
    )
    {
        var a = await source;

        if (a.HasError)
        {
            return a.Error;
        }

        return selector(a.Value);
    }

    public static Task<Result<B>> Select<A, B>
    (
        this Result<A> a,
        Func<A, Task<Result<B>>> selector
    )
    {
        if (a.HasError)
        {
            var b = new Result<B> { Error = a.Error };

            return Task.FromResult(b);
        }

        return selector(a.Value);
    }

    public static Result<IEnumerable<B>> Select<A, B>
    (
        this Result<IEnumerable<A>> source,
        Func<A, B> selector
    )
    {
        if (source.HasError)
        {
            return source.Error;
        }

        List<B> returnItems = [];

        foreach (var a in source.Value)
        {
            var selectorResult = selector(a);

            returnItems.Add(selectorResult);
        }

        return returnItems;
    }

    public static async Task<Result<C>> SelectMany<A, B, C>
    (
        this Task<Result<A>> source,
        Func<A, Task<Result<B>>> bind,
        Func<A, B, C> resultSelector
    )
    {
        var a = await source;

        if (a.HasError)
        {
            return a.Error;
        }

        var middle = await bind(a.Value);
        if (middle.HasError)
        {
            return middle.Error;
        }

        return resultSelector(a.Value, middle.Value);
    }

    public static async Task<Result<C>> SelectMany<A, B, C>
    (
        this Task<Result<A>> source,
        Func<A, Task<Result<B>>> bind,
        Func<A, B, Task<Result<C>>> resultSelector
    )
    {
        var a = await source;

        if (a.HasError)
        {
            return a.Error;
        }

        var middle = await bind(a.Value);
        if (middle.HasError)
        {
            return middle.Error;
        }

        return await resultSelector(a.Value, middle.Value);
    }

    public static Result<C> SelectMany<A, B, C>
    (
        this Result<A> source,
        Func<A, Result<B>> bind,
        Func<A, B, C> resultSelector
    )
    {
        if (source.HasError)
        {
            return source.Error;
        }

        var middle = bind(source.Value);
        if (middle.HasError)
        {
            return middle.Error;
        }

        return resultSelector(source.Value, middle.Value);
    }

    public static Result<IEnumerable<C>> SelectMany<A, B, C>
    (
        this Result<A> source,
        Func<A, IEnumerable<B>> bind,
        Func<A, B, C> resultSelector
    )
    {
        if (source.HasError)
        {
            return source.Error;
        }

        try
        {
            var middles = bind(source.Value);

            var results = middles.Select(middle => resultSelector(source.Value, middle));

            return new() { Value = results };
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static Result<IEnumerable<C>> SelectMany<A, B, C>
    (
        this Result<A> source,
        Func<A, IEnumerable<Result<B>>> bind,
        Func<A, B, C> resultSelector
    )
    {
        if (source.HasError)
        {
            return source.Error;
        }

        var a = source.Value;

        var enumerable = bind(a);

        List<C> returnList = [];

        foreach (var item in enumerable)
        {
            if (item.HasError)
            {
                return item.Error;
            }

            var b = item.Value;

            var c = resultSelector(a, b);

            returnList.Add(c);
        }

        return returnList;
    }

    public static IEnumerable<Result<C>> SelectMany<A, B, C>
    (
        this IEnumerable<A> source,
        Func<A, Result<B>> bind,
        Func<A, B, C> resultSelector
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (bind == null)
        {
            throw new ArgumentNullException(nameof(bind));
        }

        if (resultSelector == null)
        {
            throw new ArgumentNullException(nameof(resultSelector));
        }

        List<Result<C>> returnItems = [];

        foreach (var a in source)
        {
            var b = bind(a);
            if (b.HasError)
            {
                returnItems.Add(new()
                {
                    Error = b.Error
                });

                return returnItems;
            }

            var c = resultSelector(a, b.Value);

            returnItems.Add(c);
        }

        return returnItems;
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>
    (
        this Result<IEnumerable<A>> source,
        Func<A, Task<Result<B>>> bindAsync,
        Func<A, B, Result<C>> selector
    )
    {
        if (source.HasError)
        {
            yield return source.Error;
        }

        foreach (var a in source.Value)
        {
            var b = await bindAsync(a);
            if (b.HasError)
            {
                yield return b.Error;
            }

            var c = selector(a, b.Value);
            if (c.HasError)
            {
                yield return c.Error;
            }

            yield return c.Value;
        }
    }

    public static async Task<Result<C>> SelectMany<A, B, C>
    (
        this Task<Result<A>> source,
        Func<A, Result<B>> bind,
        Func<A, B, C> resultSelector
    )
    {
        var a = await source;

        if (a.HasError)
        {
            return a.Error;
        }

        var middle = bind(a.Value);
        if (middle.HasError)
        {
            return middle.Error;
        }

        return resultSelector(a.Value, middle.Value);
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>
    (
        this Task<Result<A>> source,
        Func<A, IEnumerable<B>> bind,
        Func<A, B, C> resultSelector
    )
    {
        var a = await source;

        if (a.HasError)
        {
            yield return a.Error;
        }

        var enumerable = bind(a.Value);
        foreach (var b in enumerable)
        {
            yield return resultSelector(a.Value, b);
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>
    (
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
            }

            var b = await bind(a.Value);
            if (b.HasError)
            {
                yield return b.Error;
            }

            yield return resultSelector(a.Value, b.Value);
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>
    (
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
            }

            var b = bind(a.Value);
            if (b.HasError)
            {
                yield return b.Error;
            }

            yield return resultSelector(a.Value, b.Value);
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>
    (
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
            }

            var b = await bind(a.Value);
            if (b.HasError)
            {
                yield return b.Error;
            }

            yield return await resultSelector(a.Value, b.Value);
        }
    }

    public static async IAsyncEnumerable<Result<C>> SelectMany<A, B, C>
    (
        this IEnumerable<A> source,
        Func<A, Task<Result<B>>> bind,
        Func<A, B, C> resultSelector
    )
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (bind == null)
        {
            throw new ArgumentNullException(nameof(bind));
        }

        if (resultSelector == null)
        {
            throw new ArgumentNullException(nameof(resultSelector));
        }

        foreach (var a in source)
        {
            var b = await bind(a);
            if (b.HasError)
            {
                yield return b.Error;
            }

            yield return resultSelector(a, b.Value);
        }
    }

    public static IEnumerable<Result<A>> Where<A>
    (
        this IEnumerable<Result<A>> source,
        Func<A, bool> predicate
    )
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        List<Result<A>> returnList = [];

        foreach (var result in source)
        {
            if (result.HasError)
            {
                returnList.Add(new() { Error = result.Error });

                return returnList;
            }

            if (predicate(result.Value))
            {
                returnList.Add(result);
            }
        }

        return returnList;
    }
}