namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    public static IAsyncEnumerable<Result<Unit>> TryExport()
    {
        var fileModels =
            from config in ConfigReader.ReadConfig()
            from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
            from typeDefinition in CecilHelper.GetTypes(assemblyDefinition, config.ListOfTypes ?? [])
            select new FileModel
            {
                Path    = Path.Combine(config.OutputDirectoryPath ?? string.Empty, $"{typeDefinition.Name}.ts"),
                Content = TsOutput.LinesToString(TsOutput.GetTsCode(TsModelCreator.CreatFrom(typeDefinition)))
            };

        return
            from fileModel in fileModels
            from syncedFile in TrySyncWithLocalFileSystem(fileModel)
            select FileSystem.Save(syncedFile);
    }

    static async Task<Result<FileModel>> TrySyncWithLocalFileSystem(FileModel file)
    {
        if (File.Exists(file.Path))
        {
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

        return file;
    }
}