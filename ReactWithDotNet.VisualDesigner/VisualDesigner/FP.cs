global using FunctionalUtilities;
global using static FunctionalUtilities.FP;

namespace FunctionalUtilities;

public sealed class Result
{
    public Exception Error { get; init; }

    public bool HasError { get; init; }

    public bool Success { get; init; }

    public static Result<T> From<T>(T value)
    {
        return new() { Success = true, Value = value };
    }

    public static implicit operator Result(Exception failInfo)
    {
        return new() { HasError = true, Error = failInfo };
    }

    public static implicit operator Result(NoneObject noneObject)
    {
        return new() { Success = true };
    }
}

public class Result<TValue>
{
    public Exception Error { get; init; }

    public bool HasError => !Success;

    public bool Success { get; init; }

    public TValue Value { get; init; }

    public static implicit operator Result<TValue>(TValue value)
    {
        return new() { Value = value, Success = true };
    }

    public static implicit operator Result<TValue>(Exception failInfo)
    {
        return new() { Error = failInfo };
    }

    public static implicit operator Result<TValue>(NoneObject noneObject)
    {
        return new() { Success = true };
    }
}

public sealed class NotNullResult<TValue>
{
    public Exception Error { get; init; }

    public bool HasError { get; init; }

    public bool Success { get; init; }

    public TValue Value { get; init; }

    public static implicit operator NotNullResult<TValue>(TValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new() { Value = value, Success = true };
    }

    public static implicit operator NotNullResult<TValue>(Exception failInfo)
    {
        return new() { HasError = true, Error = failInfo };
    }
}

public sealed record Maybe<TValue>
{
    public bool HasNoValue => !HasValue;

    public bool HasValue { get; private init; }

    public TValue Value { get; init; }

    public static implicit operator Maybe<TValue>(TValue value)
    {
        return new() { Value = value, HasValue = value is not null };
    }

    public static implicit operator Maybe<TValue>(NoneObject noneObject)
    {
        return new() { HasValue = false };
    }

    public static Maybe<TValue> Some(TValue value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new() { Value = value, HasValue = true };
    }

    public override string ToString()
    {
        return HasValue ? $"Some({Value})" : "None";
    }
}

public sealed class NoneObject
{
    public static readonly NoneObject Instance = new();

    NoneObject()
    {
    }
}

static class FP
{
    public static readonly Result Success = new() { Success = true };

    public static NoneObject None => NoneObject.Instance;

    public static Result<IReadOnlyList<B>> ConvertAll<A, B>(this IEnumerable<NotNullResult<A>> response, Func<A, Result<B>> convertFunc)
    {
        List<B> values = [];

        foreach (var result in response)
        {
            if (result.HasError)
            {
                return result.Error;
            }

            var resultB = convertFunc(result.Value);
            if (resultB.HasError)
            {
                return resultB.Error;
            }

            values.Add(resultB.Value);
        }

        return values;
    }

    public static Result Fail(string message)
    {
        return new() { Success = false, HasError = true, Error = new(message) };
    }

    public static Result FoldThen<A>(this IEnumerable<Result<IReadOnlyList<A>>> response, Action<IReadOnlyList<A>> nextAction)
    {
        List<A> values = [];

        foreach (var result in response)
        {
            if (result.HasError)
            {
                return result.Error;
            }

            values.AddRange(result.Value);
        }

        nextAction(values);

        return Success;
    }

    public static void HasValue<TValue>(this Maybe<TValue> maybe, Action<TValue> action)
    {
        if (maybe.HasNoValue)
        {
            return;
        }

        action(maybe.Value);
    }
    
    public static Maybe<B> HasValue<A,B>(this Maybe<A> maybe, Func<A, B> convertFunc)
    {
        if (maybe.HasNoValue)
        {
            return None;
        }

        return convertFunc(maybe.Value);
    }
    
    public static bool Is<A, B>(this Maybe<(A, B)> maybe, (A a, B b) value)
    {
        if (maybe.HasNoValue)
        {
            return false;
        }

        return EqualityComparer<A>.Default.Equals(maybe.Value.Item1, value.a) &&
               EqualityComparer<B>.Default.Equals(maybe.Value.Item2, value.b);
    }
    public static bool Is<A, B>(this Maybe<(A, B)> maybe, A a, B b)
    {
        if (maybe.HasNoValue)
        {
            return false;
        }

        return EqualityComparer<A>.Default.Equals(maybe.Value.Item1, a) &&
               EqualityComparer<B>.Default.Equals(maybe.Value.Item2, b);
    }

    public static Result<B> Then<A, B>(this (A value, Exception exception) result, Func<A, B> convertFunc)
    {
        if (result.exception is not null)
        {
            return result.exception;
        }

        return convertFunc(result.value);
    }

    public static Result<B> Then<A, B>(this Result<A> result, Func<A, B> convertFunc)
    {
        if (result.HasError)
        {
            return result.Error;
        }

        return convertFunc(result.Value);
    }

    public static Result Then<A>(this Result<A> result, Action<A> action)
    {
        if (result.HasError)
        {
            return result.Error;
        }

        action(result.Value);

        return None;
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

    public static Result<B> Then<A, B>(this IEnumerable<Result<A>> response, Func<IReadOnlyList<A>, B> nextAction)
    {
        List<A> values = [];

        foreach (var result in response)
        {
            if (result.HasError)
            {
                return result.Error;
            }

            values.Add(result.Value);
        }

        return nextAction(values);
    }

    public static Result<IReadOnlyList<TValue>> ToReadOnlyList<TValue>(this Result<TValue> result)
    {
        return result.Then(x => (IReadOnlyList<TValue>) [x]);
    }

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
}