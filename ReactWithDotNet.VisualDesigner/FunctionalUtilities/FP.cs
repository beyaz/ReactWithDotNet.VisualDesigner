global using ReactWithDotNet.VisualDesigner.FunctionalUtilities;
global using static ReactWithDotNet.VisualDesigner.FunctionalUtilities.FP;
using System.Collections;

namespace ReactWithDotNet.VisualDesigner.FunctionalUtilities;

public sealed class Unit
{
    public static readonly Unit Value = new();
}

public sealed record Result<TValue>
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

    public static implicit operator Result<TValue>(NoneObject _)
    {
        return new() { Success = true };
    }

    public static implicit operator Result<TValue>(Result<Unit> value)
    {
        return new() { Success = value.Success, Error = value.Error };
    }
}

public sealed record Maybe<TValue> : IEnumerable<TValue>
{
    public bool HasNoValue => !HasValue;

    public bool HasValue { get; private init; }

    public TValue Value { get; init; }

    public static implicit operator Maybe<TValue>(TValue value)
    {
        return new() { Value = value, HasValue = value is not null };
    }

    public static implicit operator Maybe<TValue>((bool success, TValue value) tuple)
    {
        return new() { Value = tuple.value, HasValue = tuple.success };
    }

    public static implicit operator Maybe<TValue>(NoneObject _)
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

    public IEnumerator<TValue> GetEnumerator()
    {
        if (HasValue)
        {
            yield return Value;
        }
    }

    public override string ToString()
    {
        return HasValue ? $"Some({Value})" : "None";
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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
    public static NoneObject None => NoneObject.Instance;

    public static Result<Unit> Fail(string message)
    {
        return new() { Success = false, Error = new(message) };
    }

    public static Result<T> Fail<T>(Exception exception)
    {
        return new() { Success = false, Error = exception };
    }

    public static Exception GetError<T>(this IEnumerable<Result<T>> results)
    {
        foreach (var result in results)
        {
            if (result.HasError)
            {
                return result.Error;
            }
        }

        return new ArgumentException("results has no error.");
    }

    public static bool HasError<T>(this IEnumerable<Result<T>> results)
    {
        foreach (var result in results)
        {
            if (result.HasError)
            {
                return true;
            }
        }

        return false;
    }

    public static void HasValue<TValue>(this Maybe<TValue> maybe, Action<TValue> action)
    {
        if (maybe.HasNoValue)
        {
            return;
        }

        action(maybe.Value);
    }

    public static async Task<Result<T3>> Pipe<T0, T1, T2, T3>(T0 i0, Func<T0, Task<Result<T1>>> m0, Func<T1, T2> m1, Func<T2, T3> m2)
    {
        var response0 = await m0(i0);
        if (response0.HasError)
        {
            return response0.Error;
        }

        var response1 = m1(response0.Value);

        var response2 = m2(response1);

        return response2;
    }

    public static Result<T> ResultFrom<T>(T value)
    {
        return new() { Success = true, Value = value };
    }

    public static async Task<Result<TValue>> RunWhile<TValue>(TValue value, Func<TValue, bool> canContinueToExecute, Pipe<TValue, TValue> pipe)
    {
        foreach (Func<TValue, Task<Result<TValue>>> item in pipe)
        {
            if (!canContinueToExecute(value))
            {
                return value;
            }

            var result = await item(value);
            if (result.HasError)
            {
                return result.Error;
            }

            value = result.Value;
        }

        return value;
    }

    public static Result<B> Then<A, B>(this (A value, Exception exception) result, Func<A, Result<B>> convertFunc)
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

    public static Result<A> Then<A>(this Result<Unit> result, Func<A> onSuccessFunc)
    {
        if (result.HasError)
        {
            return result.Error;
        }

        return onSuccessFunc();
    }
}

public sealed record Pipe<Tin, Tout> : IEnumerable<Func<Tin, Task<Result<Tout>>>>
{
    readonly List<Func<Tin, Task<Result<Tout>>>> _items = [];

    public void Add(Func<Tin, Tout> value)
    {
        var fn = (Tin x) => Task.FromResult(ResultFrom(value(x)));

        _items.Add(fn);
    }

    public void Add(Func<Tin, Task<Result<Tout>>> value)
    {
        _items.Add(value);
    }

    public IEnumerator GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator<Func<Tin, Task<Result<Tout>>>> IEnumerable<Func<Tin, Task<Result<Tout>>>>.GetEnumerator()
    {
        return _items.GetEnumerator();
    }
}