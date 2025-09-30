global using static BDigitalFrameworkApiToTsExporter.Extensions;


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


public sealed class Result<TSuccess>
{
    public readonly TSuccess Value;
    
    public readonly Exception Error;

    bool Success { get; }
    
    public bool HasError => !Success;

    Result(TSuccess value)
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


    public Result<TResult> Select<TResult>(Func<TSuccess, TResult> selector)
    {
        if (HasError)
        {
            return Error;
        }

        return selector(Value!);
    }

    public Result<TResult> SelectMany<TResult>(Func<TSuccess, Result<TResult>> binder)
    {
        if (HasError)
        {
            return Error;
        }

        return binder(Value!);
    }

    public Result<TResult> SelectMany<TMiddle, TResult>(
        Func<TSuccess, Result<TMiddle>> binder,
        Func<TSuccess, TMiddle, TResult> projector)
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
        Func<TSuccess, IEnumerable<TMiddle>> binder,
        Func<TSuccess, TMiddle, TResult> projector)
    {
        if (HasError)
        {
            return Error;
        }

        try
        {
            IEnumerable<TMiddle> enumerable = binder(Value!);
            
            IEnumerable<TResult> results = enumerable.Select(mid => projector(Value!, mid));
            
            return new(results); 
        }
        catch (Exception exception) 
        {
            return exception;
        }
    }
    
    
  

    
    public static implicit operator Result<TSuccess>(TSuccess value)
        => new(value);

    public static implicit operator Result<TSuccess>(Exception error)
        => new(error);
    
    
    
    
}


public readonly struct Unit
{
    public static readonly Unit Value = new();
}