global using static BDigitalFrameworkApiToTsExporter.Extensions;

using System.Collections;

namespace BDigitalFrameworkApiToTsExporter;

static class Extensions
{
    public static Exception? Run<A>(Func<Result<IReadOnlyList<A>>> first, Func<IReadOnlyList<A>, Exception?> second)
    {
        var result = first();
        if (result.HasError)
        {
            return result.Error;
        }

        return second(result.Value!);
    }
    
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

public sealed record Result<TValue> : IEnumerable<TValue?>
{
    public Exception? Error { get; private init; }

    public bool HasError { get; private init; }

    public TValue? Value { get; private init; }

    public static implicit operator Result<TValue>(TValue value)
    {
        return new() { Value = value };
    }

    
    public static implicit operator Result<TValue>(Exception? failInfo)
    {
        return new() { Error = failInfo, HasError = true };
    }

    public IEnumerator<TValue?> GetEnumerator()
    {
        if (!HasError)
        {
            yield return Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}