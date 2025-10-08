namespace BDigitalFrameworkApiToTsExporter;

static class Extensions
{
    
    public static string RemoveFromStart(this string source, string value)
    {
        if (source.StartsWith(value))
        {
            return source.Substring(value.Length);
        }
        return source;
    }

}