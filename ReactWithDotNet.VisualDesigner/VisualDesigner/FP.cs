global using FunctionalUtilities;
global using static FunctionalUtilities.FP;
using System.Collections;

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

public sealed class Response<TValue> : IEnumerable<TValue>
{
    public Exception Error { get; init; }

    public bool HasError => !Success;

    public bool Success { get; init; }

    public TValue Value { get; init; }

    public static implicit operator Response<TValue>(TValue value)
    {
        return new() { Value = value, Success = true };
    }

    public static implicit operator Response<TValue>(NoneObject noneObject)
    {
        return new() { Success = true };
    }

    public static implicit operator Response<TValue>(Exception failInfo)
    {
        return new() { Error = failInfo };
    }

    public IEnumerator<TValue> GetEnumerator()
    {
        if (Success)
        {
            yield return Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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

public sealed record Maybe<TValue> : IEnumerable<TValue>
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

    public static Maybe<B> HasValue<A, B>(this Maybe<A> maybe, Func<A, B> convertFunc)
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

    public static bool Is<A>(this Maybe<A> maybe, Func<A, bool> nextFunc)
    {
        if (maybe.HasNoValue)
        {
            return false;
        }

        return nextFunc(maybe.Value);
    }

    public static async Task<Result<T3>> Pipe<T0, T1, T2, T3>(T0 i0, Func<T0, Task<Response<T1>>> m0, Func<T1, T2> m1, Func<T2, T3> m2)
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

    public static async Task<Result<T6>> Pipe<T0, T1, T2, T3, T4, T5, T6>(
        T0 p0, T1 p1,
        Func<T0, T1, Task<Response<T2>>> m0,
        Func<T2, T3> m1,
        Func<T3, Task<Response<T4>>> m2,
        Func<T4, T5> m3,
        Func<T5, T6> m4)
    {
        var response0 = await m0(p0, p1);
        if (response0.HasError)
        {
            return response0.Error;
        }

        var response1 = m1(response0.Value);

        var response2 = await m2(response1);
        if (response2.HasError)
        {
            return response2.Error;
        }

        var response3 = m3(response2.Value);

        var response4 = m4(response3);

        return response4;
    }
    
    public static async Task<Response<T3>> Pipe<T0, T1, T2, T3>(
        T0 p0, T1 p1,
        Func<T0, T1, Task<Response<T2>>> m0,
        Func<T2, T3> m1)
    {
        var response0 = await m0(p0, p1);
        if (response0.HasError)
        {
            return response0.Error;
        }

        return  m1(response0.Value);
    }
    
    public static Func<T0, Task<Response<T1>>> HasValue<T0, T1>(Func<T0,Task<Response<T1>>> m0)
    {
        return async t0 =>
        {
            if (t0 is null)
            {
                return None;
            }
            return await m0(t0);
        };

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