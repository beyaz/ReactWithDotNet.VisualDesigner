global using static BDigitalFrameworkApiToTsExporter.Keys;
using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class Keys
{
    // @formatter:off
    
    public static readonly ScopeKey<IReadOnlyList<ExternalTypeInfo>> ExternalTypes = nameof(ExternalTypes);
    
    public static readonly ScopeKey<string> ProjectDirectory = nameof(ProjectDirectory);
    
    public static readonly ScopeKey<AssemblyDefinition> Assembly = nameof(AssemblyDefinition);
    
    public static readonly ScopeKey<string> ApiName = nameof(ApiName);
    
    public static readonly ScopeKey<TypeDefinition> ModelTypeDefinition = nameof(ModelTypeDefinition);
    
    public static readonly ScopeKey<TypeDefinition> ControllerTypeDefinition = nameof(ControllerTypeDefinition);
    
    public static readonly ScopeKey<MethodDefinition> ControllerMethodDefinition = nameof(ControllerMethodDefinition);
    
    // @formatter:on
}