using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    static Result<IEnumerable<FileModel>> Test(Func<Result<Config>> readConfig, 
        Func<string?,Result<Mono.Cecil.AssemblyDefinition>> readAssembly,
        Func<AssemblyDefinition, string[]?, List<TypeDefinition>> readTypes)
    {
        return
            from config in readConfig()
            from assemblyDefinition in readAssembly(config.AssemblyFilePath)
            from typeDefinition in readTypes(assemblyDefinition, config.ListOfTypes)
            select new FileModel
            {
                Path    = Path.Combine(config.OutputDirectoryPath ?? string.Empty, $"{typeDefinition.Name}.ts"),
                Content = TsOutput.LinesToString(TsOutput.GetTsCode(TsModelCreator.CreatFrom(typeDefinition)))
            };
    }
    
    
    public static Result<Unit> TryExport()
    {
        var calculatedFiles =
            from config in ConfigReader.ReadConfig()
            from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
            from typeDefinition in CecilHelper.GetTypes(assemblyDefinition, config.ListOfTypes ?? [])
            select new FileModel
            {
                Path    = Path.Combine(config.OutputDirectoryPath ?? string.Empty, $"{typeDefinition.Name}.ts"),
                Content = TsOutput.LinesToString(TsOutput.GetTsCode(TsModelCreator.CreatFrom(typeDefinition)))
            };

        return
            from files in calculatedFiles
            from fileModel in files.Select(TrySyncWithLocalFileSystem)
            select FileSystem.Save(fileModel);
    }

    static Result<FileModel> TrySyncWithLocalFileSystem(FileModel file)
    {
        if (File.Exists(file.Path))
        {
            var result = FileSystem.ReadAllText(file.Path);
            if (result.HasError)
            {
                return result.Error;
            }

            var fileContentInDirectory = result.Value;

            var exportIndex = fileContentInDirectory.IndexOf("export ", StringComparison.OrdinalIgnoreCase);
            if (exportIndex > 0)
            {
                return file with
                {
                    Content = fileContentInDirectory[..exportIndex] + file.Content
                };
            }
        }

        return file;
    }
}