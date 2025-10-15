global using static BDigitalFrameworkApiToTsExporter.Keys;

namespace BDigitalFrameworkApiToTsExporter;

static class Keys
{
    public static readonly ScopeKey<string> ProjectDirectory = new() { Key = nameof(ProjectDirectory) };
}