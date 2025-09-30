using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class CecilHelper
{
    public static Result<AssemblyDefinition> ReadAssemblyDefinition(string? assemblyFilePath)
    {
        var primarySearchDirectoryPath = Path.GetDirectoryName(assemblyFilePath) ?? Directory.GetCurrentDirectory();

        const string secondarySearchDirectoryPath = @"d:\boa\server\bin";

        var resolver = new CustomAssemblyResolver(primarySearchDirectoryPath, secondarySearchDirectoryPath);

        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = resolver
        };

        var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFilePath, readerParameters);

        if (assemblyDefinition is null)
        {
            return new Exception("AssemblyNotFound:" + assemblyFilePath);
        }

        return assemblyDefinition;
    }

    class CustomAssemblyResolver : BaseAssemblyResolver
    {
        readonly string[] _searchDirectories;

        public CustomAssemblyResolver(params string[] searchDirectories)
        {
            _searchDirectories = searchDirectories;
            foreach (var directory in _searchDirectories)
            {
                AddSearchDirectory(directory);
            }
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            foreach (var directory in _searchDirectories)
            {
                var filePath = Path.Combine(directory, name.Name + ".dll");
                if (File.Exists(filePath))
                {
                    return AssemblyDefinition.ReadAssembly(filePath, parameters);
                }
            }

            // Eğer burada bulamazsa, varsayılan çözümleyiciye dön
            return base.Resolve(name, parameters);
        }
    }
}