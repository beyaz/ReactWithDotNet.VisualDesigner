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
    
    // @formatter:on
}

public static class ResultExtensions
{
    public static Result<TResult> Select<TSource, TResult>
    (
        this Result<TSource> result,
        Func<TSource, TResult> selector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        return selector(result.Value);
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
}