namespace Toolbox;

public static class MaybeResultExtensions
{
    public static Result<Maybe<C>> SelectMany<A, B, C>(
        this Result<A> source,
        Func<A, Maybe<B>> bind,
        Func<A, B, C> resultSelector
    )
    {
        if (source.HasError)
        {
            return source.Error;
        }

        var maybe = bind(source.Value);
        if (maybe.HasNoValue)
        {
            return new() { Value = None };
        }

        Maybe<C> c = resultSelector(source.Value, maybe.Value);

        return c;
    }
}