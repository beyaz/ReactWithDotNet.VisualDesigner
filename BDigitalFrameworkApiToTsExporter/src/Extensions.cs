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
    public readonly TSuccess Value;
    
    public readonly TError Error;

    bool Success { get; }
    
    public bool HasError => !Success;

    protected Result(TSuccess value)
    {
        Success = true;
        
        Value  = value;
        
        Error = default!;
    }

    protected Result(TError error)
    {
        Success = false;
        
        Error    = error;
        
        Value = default!;
    }


    // --- LINQ desteği ---
    public Result<TResult, TError> Select<TResult>(Func<TSuccess, TResult> selector)
        => Success ? selector(Value!)
            : Error!;

    public Result<TResult, TError> SelectMany<TResult>(Func<TSuccess, Result<TResult, TError>> binder)
        => Success ? binder(Value!)
            : Error!;

    public Result<TResult, TError> SelectMany<TMiddle, TResult>(
        Func<TSuccess, Result<TMiddle, TError>> binder,
        Func<TSuccess, TMiddle, TResult> projector)
    {
        if (!Success) return Error!;
        var mid = binder(Value!);
        return mid.Success
            ? projector(Value!, mid.Value!)
            : mid.Error!;
    }

    // --- Implicit operators ---
    public static implicit operator Result<TSuccess, TError>(TSuccess value)
        => new(value);

    public static implicit operator Result<TSuccess, TError>(TError error)
        => new(error);
}

public sealed class Result<TSuccess> : Result<TSuccess, Exception>
{
    
    public Result(TSuccess value) : base(value) { }
    
    public Result(Exception error) : base(error) { }

    public static implicit operator Result<TSuccess>(TSuccess value)
        => new(value);

    public static implicit operator Result<TSuccess>(Exception error)
        => new(error);
}
