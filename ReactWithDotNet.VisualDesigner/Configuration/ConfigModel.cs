global using static ReactWithDotNet.VisualDesigner.Configuration.Extensions;
global using  ReactWithDotNet.VisualDesigner.Configuration;

using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace ReactWithDotNet.VisualDesigner.Configuration;

sealed record ConfigModel
{
    public string BrowserExeArguments { get; init; }
    public string BrowserExePath { get; init; }
    public bool HideConsoleWindow { get; init; }
    public int NextAvailablePortFrom { get; init; }
    public bool UseUrls { get; init; }
}

static class Extensions
{
    
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

    static ConfigModel ReadConfig()
    {
        var config = YamlHelper.DeserializeFromYaml<ConfigModel>(File.ReadAllText(Path.Combine(AppFolder, "Config.yaml")));

        if (IsRunningInVS)
        {
            config = config with { UseUrls = false };
        }

        return config;
    }
}



