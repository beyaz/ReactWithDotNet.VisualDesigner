namespace Toolbox;

public static class PipeOperator
{
    extension<Tin, Tout>(Tin)
    {
        public static Tout operator |(Tin source, Func<Tin, Tout> func)
        {
            return func(source);
        }
    }
}