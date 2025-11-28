namespace Toolbox;

public static class PipeOperator
{
    extension<T, TResult>(T)
    {
        public static TResult operator |(T source, Func<T, TResult> func)
        {
            return func(source);
        }
    }
}