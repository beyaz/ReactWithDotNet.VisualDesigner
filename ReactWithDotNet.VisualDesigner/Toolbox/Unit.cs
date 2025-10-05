namespace Toolbox;

public sealed class Unit
{
    public static readonly Unit Value = new();
}

public static class UnitExtensions
{
    public static Result<Unit> SelectMany<A, B>
    (
        this Result<A> result,
        Func<A, IEnumerable<Result<B>>> binder,
        Func<A, B, Result<Unit>> projector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        var a = result.Value;

        var enumerable = binder(a);

        Result<Unit> resultC = Unit.Value;

        foreach (var item in enumerable)
        {
            if (item.HasError)
            {
                return item.Error;
            }

            var b = item.Value;

            resultC = projector(a, b);
            if (resultC.HasError)
            {
                return resultC.Error;
            }
        }

        return resultC;
    }

    public static Result<Unit> SelectMany<A, B, C>
    (
        this Result<IEnumerable<A>> result,
        Func<A, Result<B>> binder,
        Func<A, B, C> projector
    )
    {
        if (result.HasError)
        {
            return result.Error;
        }

        foreach (var a in result.Value)
        {
            var b = binder(a);
            if (b.HasError)
            {
                return b.Error;
            }
        }

        return Unit.Value;
    }
}