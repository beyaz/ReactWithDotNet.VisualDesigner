global using static ReactWithDotNet.VisualDesigner.Configuration.Extensions;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ReactWithDotNet.VisualDesigner.Configuration;

public sealed record DatabaseConfig
{
    public bool IsSQLite { get; init; }
    
    public bool IsSQLServer { get; init; }

    public bool IsMySQL { get; init; }
    
    public string ConnectionString { get; init; }
    
    public string SchemaName { get; init; }
}

public sealed record ConfigModel
{
    // @formatter:off

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Browsers { get; init; }
    
    public string BrowserExeArguments { get; init; }
    
    public bool HideConsoleWindow { get; init; }
    
    public int NextAvailablePortFrom { get; init; }
    
    public bool UseUrls { get; init; }

    public DatabaseConfig Database { get; init; }
    
    // @formatter:on
}

static class Extensions
{
    public static void TryStartBrowser(int port)
    {
        IReadOnlyList<string> browserApplicationPaths = null;
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                browserApplicationPaths = Config.Browsers["windows"];
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                browserApplicationPaths = Config.Browsers["mac"];
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                browserApplicationPaths = Config.Browsers["linux"];
            }

            if (browserApplicationPaths is null)
            {
                return;
            }
        }
        
        foreach (var applicationFilePath in browserApplicationPaths)
        {
            if (File.Exists(applicationFilePath))
            {
                IgnoreException(() => Process.Start(applicationFilePath, Config.BrowserExeArguments.Replace("{Port}", port.ToString())));
                return;
            }
        }
    }

    public static ConfigModel Config;

    static readonly bool IsRunningInVS = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VisualStudioEdition"));

    public static void IgnoreException(Action action)
    {
        try
        {
            action();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static Result<ConfigModel> ReadConfig()
    {
        var appFolder = AppContext.BaseDirectory;

        var configFilePath = Path.Combine(appFolder, "Config.yaml");
        if (!File.Exists(configFilePath))
        {
            return new ArgumentException($"Config not found.{configFilePath}");
        }
        
        var config = DeserializeFromYaml<ConfigModel>(File.ReadAllText(configFilePath));

        if (IsRunningInVS)
        {
            config = config with { UseUrls = false };
        }
        
        return Plugin.AfterReadConfig(config);
    }
}