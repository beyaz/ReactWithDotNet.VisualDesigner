using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class CecilHelper
{
    public static Result<TypeDefinition> GetType(AssemblyDefinition assemblyDefinition, string fullTypeName)
    {
        var query =
            from module in assemblyDefinition.Modules
            from type in module.Types
            where type.FullName == fullTypeName
            select type;

        foreach (var typeDefinition in query)
        {
            return typeDefinition;
        }

        return new MissingMemberException(fullTypeName);
    }

    public static Result<AssemblyDefinition> ReadAssemblyDefinition(string assemblyFilePath)
    {
        const string secondarySearchDirectoryPath = @"d:\boa\server\bin";

        var cacheKey = $"{nameof(ReadAssemblyDefinition)}-{assemblyFilePath}";

        return Cache.AccessValue(cacheKey, () => CecilAssemblyReader.ReadAssembly(assemblyFilePath, secondarySearchDirectoryPath));
    }
}