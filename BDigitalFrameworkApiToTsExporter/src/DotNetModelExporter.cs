using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    public static IAsyncEnumerable<Result<Unit>> TryExport()
    {
        var fileModels =
            from config in ConfigReader.ReadConfig()
            from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
            from typeDefinition in CecilHelper.GetTypes(assemblyDefinition, config.ListOfTypes ?? [])
            let outputFilePath = GetOutputFilePath(config, typeDefinition)
            select new FileModel
            {
                Path    = "",
                Content = TsOutput.LinesToString(TsOutput.GetTsCode(TsModelCreator.CreateFrom(typeDefinition)))
            };

        return
            from fileModel in fileModels
            from syncedFile in TrySyncWithLocalFileSystem(fileModel)
            select FileSystem.Save(syncedFile);
    }

    static Result<string> GetOutputFilePath(Config config,TypeDefinition typeDefinition)
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