namespace Toolbox;

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

    public static Result<B> Select<A, B>
    (
        this Result<A> result,
        Func<A, B> selector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        return selector(result.Value);
    }

    public static Result<B> Select<A, B>
    (
        this Result<A> result,
        Func<A, Result<B>> selector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        return selector(result.Value);
    }

    public static async Task<Result<B>> Select<A, B>
    (
        this Task<Result<A>> result,
        Func<A, B> selector
    )
    {
        var a = await result;

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

    public static Result<IEnumerable<B>> Select<TSource, B>
    (
        this Result<IEnumerable<TSource>> result,
        Func<TSource, B> selector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        List<B> returnItems = [];

        foreach (var source in result.Value)
        {
            var selectorResult = selector(source);

            returnItems.Add(selectorResult);
        }

        return returnItems;
    }

    public static async Task<Result<C>> SelectMany<A, B, C>
    (
        this Task<Result<A>> result,
        Func<A, Task<Result<B>>> binder,
        Func<A, B, C> projector
    )
    {
        var a = await result;

        if (a.HasError)
        {
            return a.Error;
        }

        var middle = await binder(a.Value);
        if (middle.HasError)
        {
            return middle.Error;
        }

        return projector(a.Value, middle.Value);
    }

    public static async Task<Result<C>> SelectMany<A, B, C>
    (
        this Task<Result<A>> result,
        Func<A, Task<Result<B>>> binder,
        Func<A, B, Task<Result<C>>> projector
    )
    {
        var a = await result;

        if (a.HasError)
        {
            return a.Error;
        }

        var middle = await binder(a.Value);
        if (middle.HasError)
        {
            return middle.Error;
        }

        return await projector(a.Value, middle.Value);
    }

    public static Result<C> SelectMany<A, B, C>
    (
        this Result<A> result,
        Func<A, Result<B>> binder,
        Func<A, B, C> projector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        var middle = binder(result.Value);
        if (middle.HasError)
        {
            return middle.Error;
        }

        return projector(result.Value, middle.Value);
    }

    public static Result<IEnumerable<C>> SelectMany<A, B, C>
    (
        this Result<A> result,
        Func<A, IEnumerable<B>> binder,
        Func<A, B, C> projector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        try
        {
            var middles = binder(result.Value);

            var results = middles.Select(middle => projector(result.Value, middle));

            return new() { Value = results };
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static Result<IEnumerable<C>> SelectMany<A, B, C>
    (
        this Result<A> result,
        Func<A, IEnumerable<Result<B>>> binder,
        Func<A, B, C> projector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        var a = result.Value;

        var enumerable = binder(a);

        List<C> returnList = [];

        foreach (var item in enumerable)
        {
            if (item.HasError)
            {
                return item.Error;
            }

            var b = item.Value;

            var c = projector(a, b);

            returnList.Add(c);
        }

        return returnList;
    }

    public static Result<IEnumerable<C>> SelectMany<A, B, C>
    (
        this IEnumerable<A> result,
        Func<A, Result<B>> binder,
        Func<A, B, C> projector
    )
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (binder == null)
        {
            throw new ArgumentNullException(nameof(binder));
        }

        if (projector == null)
        {
            throw new ArgumentNullException(nameof(projector));
        }

        List<C> returnItems = [];

        foreach (var a in result)
        {
            var b = binder(a);
            if (b.HasError)
            {
                return b.Error;
            }

            var c = projector(a, b.Value);

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

    public static Result<IEnumerable<A>> Where<A>
    (
        this Result<IEnumerable<A>> result,
        Func<A, bool> filter
    )
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        if (result.HasError)
        {
            return result.Error;
        }

        return new()
        {
            Value = result.Value.Where(filter)
        };
    }
}