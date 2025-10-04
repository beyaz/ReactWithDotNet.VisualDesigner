﻿global using static ReactWithDotNet.VisualDesigner.FunctionalUtilities.MaybeExtensions;

using System.Collections;

namespace ReactWithDotNet.VisualDesigner.FunctionalUtilities;

public sealed record Maybe<TValue> : IEnumerable<TValue>
{
    public bool HasNoValue => !HasValue;

    public bool HasValue { get; private init; }

    public TValue Value { get; init; }

    public static implicit operator Maybe<TValue>(TValue value)
    {
        return new() { Value = value, HasValue = value is not null };
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


static class MaybeExtensions
{
    public static NoneObject None => NoneObject.Instance;
    
    public static void HasValue<TValue>(this Maybe<TValue> maybe, Action<TValue> action)
    {
        if (maybe.HasNoValue)
        {
            return;
        }

        action(maybe.Value);
    }
    
}