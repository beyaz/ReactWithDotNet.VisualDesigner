global using ReactWithDotNet.VisualDesigner.FunctionalUtilities;
global using static ReactWithDotNet.VisualDesigner.FunctionalUtilities.FP;
using System.Collections;

namespace ReactWithDotNet.VisualDesigner.FunctionalUtilities;

static class FP
{
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