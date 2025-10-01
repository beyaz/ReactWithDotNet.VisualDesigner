namespace BDigitalFrameworkApiToTsExporter;

public sealed class Result<TValue>
{
    public readonly Exception Error;

    public readonly TValue Value;

    internal Result(TValue value)
    {
        Success = true;
        Value   = value;
        Error   = null!;
    }

    Result(Exception error)
    {
        Success = false;
        Error   = error;
        Value   = default!;
    }

    public bool HasError => !Success;

    bool Success { get; }

    public static implicit operator Result<TValue>(TValue value)
    {
        return new(value);
    }

    public static implicit operator Result<TValue>(Exception error)
    {
        return new(error);
    }
}

public sealed class Unit
{
    public static readonly Unit Value = new();
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

    public static Result<Unit> Select<TSource>
    (
        this Result<IEnumerable<TSource>> result,
        Func<TSource, Result<Unit>> selector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        foreach (var source in result.Value)
        {
            var selectorResult = selector(source);
            if (selectorResult.HasError)
            {
                return selectorResult.Error;
            }
        }

        return Unit.Value;
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

            return new(results);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static Result<Unit> SelectMany<A, B>
    (
        this Result<A> result,
        Func<A, IEnumerable<Result<B>>> binder,
        Func<A, B, Result<Unit>> projector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        var a = result.Value;

        var enumerable = binder(a);

        Result<Unit> resultC = new(Unit.Value);

        foreach (var item in enumerable)
        {
            if (item.HasError)
            {
                return item.Error;
            }

            var b = item.Value;

            resultC = projector(a, b);
            if (resultC.HasError)
            {
                return resultC.Error;
            }
        }

        return resultC;
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

    public static Result<Unit> SelectMany<A, B, C>
    (
        this Result<IEnumerable<A>> result,
        Func<A, Result<B>> binder,
        Func<A, B, C> projector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        foreach (var a in result.Value)
        {
            var b = binder(a);
            if (b.HasError)
            {
                return b.Error;
            }
        }

        return Unit.Value;
    }
}