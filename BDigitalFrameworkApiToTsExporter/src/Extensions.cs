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
    public static IEnumerable<T> AsEnumerable<T>(this Result<T> result)
    {
        return result.HasError ? [] : [result.Value!];
    }

    // --- Normal Select (map) ---
    public static Result<TResult> Select<T, TResult>(this Result<T> r, Func<T, TResult> selector)
    {
        return r.HasError ? r.Error : selector(r.Value!);
    }

    // --- Normal SelectMany (bind) ---
    public static Result<TResult> SelectMany<T, TResult>(this Result<T> r, Func<T, Result<TResult>> binder)
    {
        return r.HasError ? r.Error : binder(r.Value!);
    }

    // --- SelectMany + projector (LINQ query syntax için) ---
    public static Result<TResult> SelectMany<T, TMiddle, TResult>(
        this Result<T> result,
        Func<T, Result<TMiddle>> binder,
        Func<T, TMiddle, TResult> projector)
    {
        if (result.HasError)
        {
            return result.Error;
        }

        var middle = binder(result.Value!);
        if (middle.HasError)
        {
            return middle.Error;
        }

        return projector(result.Value!, middle.Value!);
    }

    // --- Result + IEnumerable flatten ---
    public static Result<IEnumerable<TResult>> SelectMany<T, TMiddle, TResult>(
        this Result<T> result,
        Func<T, IEnumerable<TMiddle>> binder,
        Func<T, TMiddle, TResult> projector)
    {
        if (result.HasError)
        {
            return result.Error;
        }

        try
        {
            var middles = binder(result.Value!);

            var results = middles.Select(middle => projector(result.Value!, middle));

            return new(results);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    // --- Nested Result<IEnumerable> flatten ---
    public static Result<IEnumerable<TResult>> SelectMany<T, TResult>(
        this Result<T> result,
        Func<T, Result<IEnumerable<TResult>>> binder)
    {
        if (result.HasError)
        {
            return result.Error;
        }

        var inner = binder(result.Value!);
        if (inner.HasError)
        {
            return inner.Error;
        }

        return new(inner.Value);
    }
}