using Newtonsoft.Json;

namespace BDigitalFrameworkApiToTsExporter;

public sealed record ApiInfo
{
    public required string Name { get; init; }
}