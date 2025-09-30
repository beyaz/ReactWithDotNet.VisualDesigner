namespace BDigitalFrameworkApiToTsExporter;

static class Extensions
{
    public static Exception? Then<TIn>(this (TIn? value, Exception? exception) tuple, Func<TIn?, Exception?> nextFunc)
    {
        if (tuple.exception is not null)
        {
            return tuple.exception;
        }

        return nextFunc(tuple.value);
    }
}