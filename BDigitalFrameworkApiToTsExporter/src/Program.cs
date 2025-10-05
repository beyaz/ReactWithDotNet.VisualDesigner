namespace BDigitalFrameworkApiToTsExporter;

static class Program
{
    static async Task Main()
    {
        var result = await DotNetModelExporter.TryExport();
        if (result.HasError)
        {
            throw result.Error;
        }
    }
}