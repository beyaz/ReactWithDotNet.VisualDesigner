using System.IO;
using Mono.Cecil;

namespace Toolbox;

static class CecilAssemblyReader
{
    public static Result<AssemblyDefinition> ReadAssembly(string assemblyFilePath, string secondarySearchDirectoryPath = null)
    {
        if (!File.Exists(assemblyFilePath))
        {
            return new FileNotFoundException(assemblyFilePath);
        }

        var primarySearchDirectoryPath = Path.GetDirectoryName(assemblyFilePath) ?? Directory.GetCurrentDirectory();

        var resolver = new CustomAssemblyResolver(primarySearchDirectoryPath, secondarySearchDirectoryPath);

        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = resolver,

            InMemory = true,

            ReadWrite = false,

            ReadingMode = ReadingMode.Immediate
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
        readonly IReadOnlyList<string> _searchDirectories;

        public CustomAssemblyResolver(params string[] searchDirectories)
        {
            _searchDirectories = (from path in searchDirectories where !string.IsNullOrWhiteSpace(path) select path).AsReadOnlyList();
            
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