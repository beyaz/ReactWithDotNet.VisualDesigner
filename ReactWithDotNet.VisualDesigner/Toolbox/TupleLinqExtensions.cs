namespace Toolbox;

/// <summary>
///     Provides LINQ-style extension methods for working with tuples and other types.
/// </summary>
public static class TupleLinqExtensions
{
    public static TResult Select<T1, T2, TResult>(this (T1, T2) source, Func<(T1, T2), TResult> selector)
    {
        return selector(source);
    }

    public static TResult Select<T1, T2, T3, TResult>(this (T1, T2, T3) source, Func<(T1, T2, T3), TResult> selector)
    {
        return selector(source);
    }

    public static TResult Select<T1, T2, T3, T4, TResult>(this (T1, T2, T3, T4) source, Func<(T1, T2, T3, T4), TResult> selector)
    {
        return selector(source);
    }

    public static TResult Select<T1, T2, T3, T4, T5, TResult>(this (T1, T2, T3, T4, T5) source, Func<(T1, T2, T3, T4, T5), TResult> selector)
    {
        return selector(source);
    }
}