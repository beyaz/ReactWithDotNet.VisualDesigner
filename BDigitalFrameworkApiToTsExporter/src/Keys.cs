global using static BDigitalFrameworkApiToTsExporter.Keys;

namespace BDigitalFrameworkApiToTsExporter;

static class Keys
{
    public static readonly ScopeKey<string> ProjectDirectory = new() { Key = nameof(ProjectDirectory) };
    
    public static readonly ScopeKey<ConfigModel> Config = new() { Key = nameof(Config) };
}