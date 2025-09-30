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

public readonly struct Unit
{
    public static readonly Unit Value = new();
}

public static class ResultExtensions
{
    public static Result<B> Select<A, B>(this Result<A> result, Func<A, B> selector)
    {
        if (result.HasError)
        {
            return result.Error;
        }

        return selector(result.Value);
    }

    public static Result<B> SelectMany<A, B>(this Result<A> result, Func<A, Result<B>> binder)
    {
        if (result.HasError)
        {
            return result.Error;
        }

        return binder(result.Value);
    }

    public static Result<IEnumerable<B>> SelectMany<A, B>
    (
        this Result<A> result,
        Func<A, Result<IEnumerable<B>>> binder
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        return binder(result.Value);
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

    public static Result<C> SelectMany<A, B, C>
    (
        this Result<A> result,
        Func<A, Result<B>> binder,
        Func<A, B, Result<C>> projector
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
}