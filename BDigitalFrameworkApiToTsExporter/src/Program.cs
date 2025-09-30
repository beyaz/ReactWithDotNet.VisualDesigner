namespace BDigitalFrameworkApiToTsExporter;

static class Program
{
    static void Main()
    {
        var result = DotNetModelExporter.TryExport();
        if (result.HasError)
        {
            throw result.Error;
        }
    }
}