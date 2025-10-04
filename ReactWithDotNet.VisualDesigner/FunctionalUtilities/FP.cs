global using ReactWithDotNet.VisualDesigner.FunctionalUtilities;
global using static ReactWithDotNet.VisualDesigner.FunctionalUtilities.FP;
using System.Collections;

namespace ReactWithDotNet.VisualDesigner.FunctionalUtilities;


static class FP
{
    

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
        
        var fn = (Tin x) =>
        {
            Result<Tout> result = value(x);
            
            return Task.FromResult(result);
        };

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