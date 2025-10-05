using Newtonsoft.Json;

namespace BDigitalFrameworkApiToTsExporter;

public sealed class Config
{
    public string? AssemblyFilePath { get; set; }
    
    public string? OutputDirectoryPath { get; set; }
    
    public string[]? ListOfTypes { get; set; }
}

static class ConfigReader
{
    public static async Task<Result<Config>> ReadConfig()
    {
        var configFilePath = Path.Combine(Path.GetDirectoryName(typeof(ConfigReader).Assembly.Location) ?? string.Empty, "Config.json");
        if (File.Exists(configFilePath))
        {
            var fileContent = await File.ReadAllTextAsync(configFilePath);

            var config = JsonConvert.DeserializeObject<Config>(fileContent);
            if (config is not null)
            {
                return config;
            }
        }

        return new IOException("ConfigFileNotRead");
    }
}