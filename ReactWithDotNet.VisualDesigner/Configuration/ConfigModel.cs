global using static ReactWithDotNet.VisualDesigner.Configuration.Extensions;
using System.IO;

namespace ReactWithDotNet.VisualDesigner.Configuration;

sealed record ConfigModel
{
    // @formatter:off

    public string BrowserExePathForWindows { get; init; }
    public string BrowserExePathForMac { get; init; }
    public string BrowserExePathForLinux { get; init; }
    
    public string BrowserExeArguments { get; init; }
    
    public bool HideConsoleWindow { get; init; }
    
    public int NextAvailablePortFrom { get; init; }
    
    public bool UseUrls { get; init; }
    
    // @formatter:on
}

static class Extensions
{
    public static readonly ConfigModel Config = ReadConfig();

    static readonly bool IsRunningInVS = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VisualStudioEdition"));

    public static string AppFolder
    {
        get
        {
            var location = Path.GetDirectoryName(typeof(Extensions).Assembly.Location);
            if (location == null)
            {
                throw new ArgumentException("assembly location not found");
            }

            return location;
        }
    }

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

    static ConfigModel ReadConfig()
    {
        var config = DeserializeFromYaml<ConfigModel>(File.ReadAllText(Path.Combine(AppFolder, "Config.yaml")));

        if (IsRunningInVS)
        {
            config = config with { UseUrls = false };
        }

        return config;
    }
}