namespace BDigitalFrameworkApiToTsExporter;

public sealed class Result<TValue>
{
    public readonly Exception Error;
    public readonly TValue Value;

    Result(TValue value)
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

    public Result<T> SelectMany<T>(Func<TValue, Result<T>> binder)
    {
        if (HasError)
        {
            return Error;
        }

        return binder(Value!);
    }

    public Result<TResult> SelectMany<TMiddle, TResult>(
        Func<TValue, Result<TMiddle>> binder,
        Func<TValue, TMiddle, TResult> projector)
    {
        if (HasError)
        {
            return Error;
        }

        var middle = binder(Value!);
        if (middle.HasError)
        {
            return middle.Error;
        }

        return projector(Value!, middle.Value!);
    }

    public Result<IEnumerable<TResult>> SelectMany<TMiddle, TResult>(
        Func<TValue, IEnumerable<TMiddle>> binder,
        Func<TValue, TMiddle, TResult> projector)
    {
        if (HasError)
        {
            return Error;
        }

        try
        {
            var enumerable = binder(Value!);

            var results = enumerable.Select(mid => projector(Value!, mid));

            return new(results);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}

public readonly struct Unit
{
    public static readonly Unit Value = new();
}

static class ResultExtensions
{
    public static Result<T> Select<TValue, T>(Result<TValue> result, Func<TValue, T> selector)
    {
        if (result.HasError)
        {
            return result.Error;
        }

        return selector(result.Value!);
    }
}