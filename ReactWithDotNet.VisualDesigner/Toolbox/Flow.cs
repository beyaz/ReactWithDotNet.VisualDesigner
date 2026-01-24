using System.Diagnostics;

namespace Toolbox;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct Flow<T>
{
    Flow(T value, bool isValid)
    {
        Value   = value;
        IsValid = isValid;
    }

    public static Flow<T> Empty => new(default!, false);

    public bool IsValid { get; }

    public T Value { get; }

    string DebuggerDisplay => IsValid
        ? $"Flow<{typeof(T).Name}>: {Value}"
        : $"Flow<{typeof(T).Name}>: <empty>";

    public static Flow<T> Valid(T value)
    {
        return new(value, true);
    }
}

public static class FlowLinqExtensions
{
    // from x in <T>
    public static Flow<T> Select<T>(this T source, Func<T, T> selector)
    {
        // LINQ syntax bunu zorunlu kılar
        // burada flow sessizce başlar
        return Flow<T>.Valid(selector(source));
    }

    public static Flow<TResult> Select<T, TResult>(
        this Flow<T> flow,
        Func<T, TResult> selector)
    {
        if (!flow.IsValid)
        {
            return Flow<TResult>.Empty;
        }

        return Flow<TResult>.Valid(selector(flow.Value));
    }

    public static Flow<TResult> SelectMany<T, TIntermediate, TResult>(
        this Flow<T> flow,
        Func<T, TIntermediate> binder,
        Func<T, TIntermediate, TResult> projector)
    {
        if (!flow.IsValid)
        {
            return Flow<TResult>.Empty;
        }

        var intermediate = binder(flow.Value);

        return Flow<TResult>.Valid(projector(flow.Value, intermediate));
    }

    public static T UnwrapOrDefault<T>(this Flow<T> flow)
    {
        return flow.IsValid ? flow.Value : default;
    }

    public static T UnwrapOrThrow<T>(this Flow<T> flow, string message = null)
    {
        if (!flow.IsValid)
        {
            throw new InvalidOperationException(
                message ?? "Flow evaluation failed.");
        }

        return flow.Value;
    }

    public static Flow<T> Where<T>(this Flow<T> flow, Func<T, bool> predicate)
    {
        if (!flow.IsValid)
        {
            return flow;
        }

        return predicate(flow.Value)
            ? flow
            : Flow<T>.Empty;
    }
}