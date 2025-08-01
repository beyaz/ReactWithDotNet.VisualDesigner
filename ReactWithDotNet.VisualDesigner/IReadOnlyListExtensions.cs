﻿using System.Collections.Immutable;

namespace ReactWithDotNet.VisualDesigner;

static class IReadOnlyListExtensions
{
    public static IReadOnlyList<T> Add<T>(this IReadOnlyList<T> readOnlyList, T value)
    {
        return readOnlyList.ToImmutableList().Add(value);
    }
    
    public static IReadOnlyList<T> RemoveAt<T>(this IReadOnlyList<T> readOnlyList, int index)
    {
        return readOnlyList.ToImmutableList().RemoveAt(index);
    }
    
    public static IReadOnlyList<T> Insert<T>(this IReadOnlyList<T> readOnlyList, int index, T value)
    {
        return readOnlyList.ToImmutableList().Insert(index, value);
    }
    
    public static IReadOnlyList<T> SetItem<T>(this IReadOnlyList<T> readOnlyList, int index, T value)
    {
        return readOnlyList.ToImmutableList().SetItem(index, value);
    }
    
    public static IReadOnlyList<T> Remove<T>(this IReadOnlyList<T> readOnlyList, T value)
    {
        return readOnlyList.ToImmutableList().Remove(value);
    }
    
    public static IReadOnlyList<T> AddRange<T>(this IReadOnlyList<T> readOnlyList, IEnumerable<T> enumerable)
    {
        return readOnlyList.ToImmutableList().AddRange(enumerable);
    }
    public static IReadOnlyList<T> RemoveAll<T>(this IReadOnlyList<T> readOnlyList, Predicate<T> match)
    {
        return readOnlyList.ToImmutableList().RemoveAll(match);
    }
}