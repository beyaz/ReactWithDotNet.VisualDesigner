namespace BDigitalFrameworkApiToTsExporter;

static class Program
{
    static async Task Main2()
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