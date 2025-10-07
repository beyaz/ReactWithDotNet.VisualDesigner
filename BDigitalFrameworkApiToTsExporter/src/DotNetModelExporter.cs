using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    
    static IAsyncEnumerable<Result<FileModel>> ExportController(TypeDefinition controllerDefinition)
    {
        // publicMethod = GetPublicMethods
        // types = GetTypes (publicMethod)
        // typeFiles = calculateFiles()

        return null;
    }
    
    public static IAsyncEnumerable<Result<Unit>> TryExport()
    {
        var fileModels =
            from config in ConfigReader.ReadConfig()
            from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
            from typeDefinition in CecilHelper.GetTypes(assemblyDefinition, config.ListOfTypes ?? [])
            from outputFilePath in GetOutputFilePath(config, typeDefinition)
            let fileContent = TsOutput.LinesToString(TsOutput.GetTsCode(TsModelCreator.CreateFrom(typeDefinition)))
            select new FileModel
            {
                Path    = outputFilePath,
                Content = fileContent
            };

        return
            from fileModel in fileModels
            from syncedFile in TrySyncWithLocalFileSystem(fileModel)
            select FileSystem.Save(syncedFile);
    }
    
    static IEnumerable<PropertyDefinition> GetMappingPropertyList(TypeDefinition model, TypeDefinition apiParameter)
    {
        return
            from parameterProperty in apiParameter.Properties
            where modelHasNamedProperty(model, parameterProperty)
            select parameterProperty;
        
        static bool modelHasNamedProperty(TypeDefinition model, PropertyDefinition property)
        {
            foreach (var modelProperty in model.Properties)
            {
                if (string.Equals(modelProperty.Name, property.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    static Result<string> GetOutputFilePath(Config config, TypeDefinition typeDefinition)
    {
        return Path.Combine(config.OutputDirectoryPath ?? string.Empty, $"{typeDefinition.Name}.ts");
    }

    static async Task<Result<FileModel>> TrySyncWithLocalFileSystem(FileModel file)
    {
        if (!File.Exists(file.Path))
        {
            return file;
        }

        return await
            from fileContentInDirectory in FileSystem.ReadAllText(file.Path)
            let exportIndex = fileContentInDirectory.IndexOf("export ", StringComparison.OrdinalIgnoreCase)
            select (exportIndex > 0) switch
            {
                true => file with
                {
                    Content = fileContentInDirectory[..exportIndex] + file.Content
                },
                false => file
            };
    }
}