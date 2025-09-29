namespace b_digital_framework_type_exporter;

static class Program
{
    static void Main()
    {
        var exception = DotNetModelExporter.TryExport();
        if (exception is not null)
        {
            throw exception;
        }
    }
}