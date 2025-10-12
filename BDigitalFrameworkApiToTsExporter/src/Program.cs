namespace BDigitalFrameworkApiToTsExporter;

static class Program
{
    static async Task Main()
    {
        await foreach (var result in Exporter.TryExport())
        {
            if (result.HasError)
            {
                throw result.Error;
            }
        }
    }
}