namespace Toolbox;

/// <summary>
///     Provides LINQ-style extension methods for working with tuples and other types.
/// </summary>
public static class TupleLinqExtensions
{
    // from x in value
    public static T Select<T>(this T source, Func<T, T> selector)
    {
        return selector(source);
    }

    // select
    public static TResult Select<T, TResult>(this T source, Func<T, TResult> selector)
    {
        return selector(source);
    }

    // from x in ...
    // from y in ...
    public static TResult SelectMany<T, TIntermediate, TResult>(this T source, Func<T, TIntermediate> binder, Func<T, TIntermediate, TResult> projector)
    {
        var intermediate = binder(source);

        return projector(source, intermediate);
    }
}