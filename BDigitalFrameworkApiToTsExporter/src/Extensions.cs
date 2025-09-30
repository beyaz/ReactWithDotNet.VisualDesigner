global using static BDigitalFrameworkApiToTsExporter.Extensions;

using System.Collections;

namespace BDigitalFrameworkApiToTsExporter;

static class Extensions
{
   
    
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




public class Result<TSuccess, TError>
{
    readonly TSuccess? _success;
    
    readonly TError? _error;

    public bool Success { get; }
    
    public bool HasError => !Success;

    protected Result(TSuccess success)
    {
        Success = true;
        _success  = success;
    }

    protected Result(TError error)
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

public sealed class Result<TSuccess> : Result<TSuccess, Exception>
{
    public Result(TSuccess success) : base(success) { }
    
    public Result(Exception error) : base(error) { }

    public static implicit operator Result<TSuccess>(TSuccess value)
        => new(value);

    public static implicit operator Result<TSuccess>(Exception error)
        => new(error);
}
