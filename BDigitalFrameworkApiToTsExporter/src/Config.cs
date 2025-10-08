using Newtonsoft.Json;

namespace BDigitalFrameworkApiToTsExporter;

public sealed record ApiInfo
{
    public required string Name { get; init; }
}

public sealed record ExternalTypeInfo
{
    public required string DotNetFullTypeName { get; init; }
    
    public required string LocalName { get; init; }
    
    public required string Source { get; init; }
}


public sealed record Config
{
    public string? AssemblyFilePath { get; init; }
    
    public string? OutputDirectoryPath { get; init; }
    
    public string[]? ListOfTypes { get; init; }

    public required IReadOnlyList<ApiInfo> ApiList { get; init; }

    public required string ProjectDirectory { get; init; }
    
    public required IReadOnlyList<ExternalTypeInfo> ExternalTypes { get; init; }
}

static class ConfigReader
{
    public static async Task<Result<Config>> ReadConfig()
    {
        var configFilePath = Path.Combine(Path.GetDirectoryName(typeof(ConfigReader).Assembly.Location) ?? string.Empty, "Config.json");

        if (!File.Exists(configFilePath))
        {
            return new FileNotFoundException(configFilePath);
        }
        
        var fileContent = await File.ReadAllTextAsync(configFilePath);

        var config = JsonConvert.DeserializeObject<Config>(fileContent);
        if (config is null)
        {
            return new InvalidDataException("InvalidConfigData: "+ configFilePath);
        }
        
        return config;
    }
}