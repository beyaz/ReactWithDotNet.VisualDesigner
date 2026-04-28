namespace Toolbox;

public static class ExecutionMixins
{
    public static B ExecUntilNotNull<A, B>(A a, Func<A, B>[] methods)
    {
        foreach (var func in methods)
        {
            var result = func(a);
            if (result is not null)
            {
                return result;
            }
        }

        return default;
    }
    
    public static C Exec<A, B, C>(A a, Func<A, B> method_a_b, Func<B, C> method_b_c)
    {
        var b = method_a_b(a);

        return method_b_c(b);
    }
    
    public static C ExecUntilNotNull<A, B, C>(A a, B b, Func<A, B, C>[] methods)
    {
        foreach (var func in methods)
        {
            var result = func(a, b);
            if (result is not null)
            {
                return result;
            }
        }

        return default;
    }
    
    
    
}