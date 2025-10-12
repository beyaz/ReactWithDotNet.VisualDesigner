using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

sealed record ScopeApi
{
    public  required Config config { get; init; }
    
    public required AssemblyDefinition AssemblyDefinition { get; init; }
    
    public required ApiInfo ApiInfo { get; init; }
}