global using static BDigitalFrameworkApiToTsExporter.Extensions;

using System.Collections;

namespace BDigitalFrameworkApiToTsExporter;

static class Extensions
{
    public static Exception? Run<A>(Func<Result<IReadOnlyList<A>>> first, Func<IReadOnlyList<A>, Exception?> second)
    {
        var result = first();
        if (result.HasError)
        {
            return result.Error;
        }

        return second(result.Value!);
    }
    
    public static Exception? Then<TIn>(this (TIn? value, Exception? exception) tuple, Func<TIn?, Exception?> nextFunc)
    {
        if (tuple.exception is not null)
        {
            return tuple.exception;
        }

        return nextFunc(tuple.value);
    }
    
    public static IReadOnlyList<T> ListFrom<T>(IEnumerable<T> enumerable)=>enumerable.ToList();
}

public sealed record Result<TValue> : IEnumerable<TValue?>
{
    public Exception? Error { get; private init; }

    public bool HasError { get; private init; }

    public TValue? Value { get; private init; }

    public static implicit operator Result<TValue>(TValue value)
    {
        return new() { Value = value };
    }

    
    public static implicit operator Result<TValue>(Exception? failInfo)
    {
        return new() { Error = failInfo, HasError = true };
    }

    public IEnumerator<TValue?> GetEnumerator()
    {
        if (!HasError)
        {
            yield return Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}


public sealed class Result<TSuccess, TError>
{
    private readonly TSuccess? _success;
    
    private readonly TError? _error;

    public bool Success { get; }
    
    public bool HasError => !Success;

    private Result(TSuccess success)
    {
        Success = true;
        _success  = success;
    }

    private Result(TError error)
    {
        Success = false;
        _error    = error;
    }


    // --- LINQ desteği ---
    public Result<TResult, TError> Select<TResult>(Func<TSuccess, TResult> selector)
        => Success ? selector(_success!)
            : _error!;

    public Result<TResult, TError> SelectMany<TResult>(Func<TSuccess, Result<TResult, TError>> binder)
        => Success ? binder(_success!)
            : _error!;

    public Result<TResult, TError> SelectMany<TMiddle, TResult>(
        Func<TSuccess, Result<TMiddle, TError>> binder,
        Func<TSuccess, TMiddle, TResult> projector)
    {
        if (!Success) return _error!;
        var mid = binder(_success!);
        return mid.Success
            ? projector(_success!, mid._success!)
            : mid._error!;
    }

    // --- Implicit operators ---
    public static implicit operator Result<TSuccess, TError>(TSuccess value)
        => new(value);

    public static implicit operator Result<TSuccess, TError>(TError error)
        => new(error);
}

