global using ReactWithDotNet.VisualDesigner.FunctionalUtilities;
global using static ReactWithDotNet.VisualDesigner.FunctionalUtilities.FP;


using System.Collections;

namespace ReactWithDotNet.VisualDesigner.FunctionalUtilities;



public sealed record Result
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

    public static implicit operator Result(NoneObject _)
    {
        return new() { Success = true };
    }
}



public sealed record Result<TValue>: IEnumerable<TValue>
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
    public static readonly Result Success = new() { Success = true };

    public static NoneObject None => NoneObject.Instance;

    public static Result<IReadOnlyList<B>> ConvertAll<A, B>(this IEnumerable<Result<A>> response, Func<A, Result<B>> convertFunc)
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

    public static async Task<Result<T6>> Pipe<T0, T1, T2, T3, T4, T5, T6>(
        T0 p0, T1 p1,
        Func<T0, T1, Task<Result<T2>>> m0,
        Func<T2, T3> m1,
        Func<T3, Task<Result<T4>>> m2,
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
    
    public static async Task<Result<T3>> Pipe<T0, T1, T2, T3>(
        T0 p0, T1 p1,
        Func<T0, T1, Task<Result<T2>>> m0,
        Func<T2, T3> m1)
    {
        var response0 = await m0(p0, p1);
        if (response0.HasError)
        {
            return response0.Error;
        }

        return  m1(response0.Value);
    }
    
    public static async Task<Result<T3>> Pipe<T0, T1, T2, T3>(T0 p0, T1 p1, Func<T0, T1, Task<Result<T2>>> m0, Func<T2, Task<Result<T3>>> m1)
    {
        var response0 = await m0(p0, p1);
        if (response0.HasError)
        {
            return response0.Error;
        }

        return await m1(response0.Value);
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
    
    public static Result<TResult> SelectMany<TSource, TIntermediate, TResult>(this Result<TSource> source, Func<TSource, Result<TIntermediate>> bind, Func<TSource, TIntermediate, TResult> project)
    {
        if (source.HasError)
        {
            return new() { Error = source.Error };
        }

        var intermediate = bind(source.Value);
        if (intermediate.HasError)
        {
            return new() { Error = intermediate.Error };
        }

        return project(source.Value, intermediate.Value);
    }
    
    public static Result<TResult> Select<TSource, TResult>(this Result<TSource> result, Func<TSource, TResult> selector)
    {
        if (result.HasError)
        {
            return new() { Error = result.Error };
        }

        return selector(result.Value);
    }
    
    
    public static Maybe<TResult> SelectMany<TSource, TIntermediate, TResult>(this Maybe<TSource> source, Func<TSource, Maybe<TIntermediate>> bind, Func<TSource, TIntermediate, TResult> project)
    {
        if (source.HasNoValue)
        {
            return None;
        }

        var intermediate = bind(source.Value);
        if (intermediate.HasNoValue)
        {
            return None;
        }

        return project(source.Value, intermediate.Value);
    }
    
    public static Maybe<TResult> Select<TSource, TResult>(this Maybe<TSource> result, Func<TSource, TResult> selector)
    {
        if (result.HasNoValue)
        {
            return None;
        }

        return selector(result.Value);
    }
   
    public static async Task<Result<TValue>> RunWhile<TValue>( TValue value, Func<TValue, bool> canContinueToExecute, Pipe<TValue, TValue> pipe)
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
    
}

public sealed record Pipe<Tin, Tout>: IEnumerable<Func<Tin, Task<Result<Tout>>>>
{
    readonly List<Func<Tin, Task<Result<Tout>>>> _items = [];
    
    public void Add(Func<Tin, Tout> value)
    {
        var fn = (Tin x) => Task.FromResult(Result.From(value(x)));
        
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