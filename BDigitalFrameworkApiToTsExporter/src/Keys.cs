global using static BDigitalFrameworkApiToTsExporter.Keys;
using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class Keys
{
    // @formatter:off
    
    public static readonly ScopeKey<IReadOnlyList<ExternalTypeInfo>> ExternalTypes = new() { Key = nameof(ExternalTypes) };
    
    public static readonly ScopeKey<string> ProjectDirectory = new() { Key = nameof(ProjectDirectory) };
    
    public static readonly ScopeKey<AssemblyDefinition> Assembly = new() { Key = nameof(AssemblyDefinition) };
    
    public static readonly ScopeKey<ApiInfo> Api = new() { Key = nameof(ApiInfo) };
    
    public static readonly ScopeKey<TypeDefinition> ModelTypeDefinition = new() { Key = nameof(ModelTypeDefinition) };
    
    public static readonly ScopeKey<TypeDefinition> ControllerTypeDefinition = new() { Key = nameof(ControllerTypeDefinition) };
    
    public static readonly ScopeKey<MethodDefinition> ControllerMethodDefinition = new() { Key = nameof(ControllerMethodDefinition) };
    
    // @formatter:on
}