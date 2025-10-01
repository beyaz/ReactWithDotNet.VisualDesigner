using Newtonsoft.Json;

namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    public static Result<Unit> TryExport()
    {
        return
            from files in CalculateFiles()
            from fileModel in files.Select(trySyncWithLocalFileSystem)
            select FileSystem.Save(fileModel);

        static Result<FileModel> trySyncWithLocalFileSystem(FileModel file)
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

    static Result<IEnumerable<FileModel>> CalculateFiles()
    {
        return
            from config in ReadConfig()
            from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
            from typeDefinition in CecilHelper.GetTypes(assemblyDefinition, config.ListOfTypes ?? [])
            select new FileModel
            {
                Path    = Path.Combine(config.OutputDirectoryPath ?? string.Empty, $"{typeDefinition.Name}.ts"),
                Content = TsOutput.LinesToString(TsOutput.GetTsCode(TsModelCreator.CreatFrom(typeDefinition)))
            };

        static Result<Config> ReadConfig()
        {
            var configFilePath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? string.Empty, "Config.json");
            if (File.Exists(configFilePath))
            {
                var fileContent = File.ReadAllText(configFilePath);

                var config = JsonConvert.DeserializeObject<Config>(fileContent);
                if (config is not null)
                {
                    return config;
                }
            }

            return new IOException("ConfigFileNotRead");
        }
    }
}