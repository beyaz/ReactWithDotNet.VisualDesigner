namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    public static Result<Unit> TryExport()
    {
        var fileModels =
            from files in
                from config in ConfigReader.ReadConfig()
                from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
                from typeDefinition in CecilHelper.GetTypes(assemblyDefinition, config.ListOfTypes ?? [])
                select new FileModel
                {
                    Path    = Path.Combine(config.OutputDirectoryPath ?? string.Empty, $"{typeDefinition.Name}.ts"),
                    Content = TsOutput.LinesToString(TsOutput.GetTsCode(TsModelCreator.CreatFrom(typeDefinition)))
                }
            from fileModel in files.Select(TrySyncWithLocalFileSystem)
            select fileModel;
        
        return
            from files in
                from config in ConfigReader.ReadConfig()
                from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
                from typeDefinition in CecilHelper.GetTypes(assemblyDefinition, config.ListOfTypes ?? [])
                select new FileModel
                {
                    Path    = Path.Combine(config.OutputDirectoryPath ?? string.Empty, $"{typeDefinition.Name}.ts"),
                    Content = TsOutput.LinesToString(TsOutput.GetTsCode(TsModelCreator.CreatFrom(typeDefinition)))
                }
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