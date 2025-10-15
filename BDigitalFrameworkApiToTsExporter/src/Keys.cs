global using static BDigitalFrameworkApiToTsExporter.Keys;
using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class Keys
{
    public static readonly ScopeKey<AssemblyDefinition> Assembly = new() { Key = nameof(AssemblyDefinition) };
    
    public static readonly ScopeKey<ConfigModel> Config = new() { Key = nameof(Config) };
    
    public static readonly ScopeKey<ApiInfo> Api = new() { Key = nameof(ApiInfo) };
}