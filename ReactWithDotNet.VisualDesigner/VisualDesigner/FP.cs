namespace ReactWithDotNet.VisualDesigner;

public sealed class Result
{
    public Exception Error { get; init; }

    public bool HasError { get; init; }

    public bool Success { get; init; }
    
    public static implicit operator Result(Exception failInfo)
    {
        return new() { HasError = true, Error = failInfo };
    }

    public static Result<T> From<T>(T value)
    {
        return new() { Success = true, Value = value };
    }
}

public sealed class Result<TValue>
{
    public Exception Error { get; init; }

    public bool HasError { get; init; }

    public bool Success { get; init; }
    
    public TValue Value { get; init; }

    public static implicit operator Result<TValue>(TValue value)
    {
        return new() { Value = value, Success = true };
    }

    public static implicit operator Result<TValue>(Exception failInfo)
    {
        return new() { HasError = true, Error = failInfo };
    }
}

public sealed record Maybe<TValue>
{
    public  TValue Value { get; init; }
    
    public bool HasValue { get; private init; }

    public static implicit operator Maybe<TValue>(TValue value)
    {
        return Some(value);
    }

    public static Maybe<TValue> Some(TValue value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }
        
        return new() { Value = value, HasValue = true };
    }
    
    public static implicit operator Maybe<TValue>(NoneObject noneObject)
    {
        return new() { HasValue = false };
    }


    public TResult Match<TResult>(Func<TValue, TResult> onSome, Func<TResult> onNone)
    {
        return HasValue ? onSome(Value) : onNone();
    }

    public Maybe<TResult> Bind<TResult>(Func<TValue, Maybe<TResult>> func)
    {
        return HasValue ? func(Value) : None;
    }

    public TValue GetValueOrDefault(TValue defaultValue = default)
    {
        return HasValue ? Value : defaultValue;
    }

    public override string ToString() => HasValue ? $"Some({Value})" : "None";
}

public sealed class NoneObject
{
    private NoneObject()
    {
        
    }
    
    public static readonly NoneObject Instance = new();
}

static class FP
{
    public static NoneObject None => NoneObject.Instance;
    
    public static readonly Result Success = new() { Success = true };
    
    public static  Result Fail(string message) => new() { Success = false, HasError = true, Error = new Exception(message)};
    
    public static async Task<TValue> Unwrap<TValue>(this Task<Result<TValue>> responseTask)
    {
        var response = await responseTask;

        if (response.Success)
        {
            return response.Value;
        }

        throw response.Error;
    }

    public static TValue Unwrap<TValue>(Result<TValue> result)
    {
        if (result.Success)
        {
            return result.Value;
        }

        throw result.Error;
    }
    
    public static async Task<Result<TValue>> Then<TValue>(this Task<Result<TValue>> response, Action<TValue> nextAction)
    {
        var value = await response;

        if (value.Success)
        {
            nextAction(value.Value);
        }

        return value;
    }
}