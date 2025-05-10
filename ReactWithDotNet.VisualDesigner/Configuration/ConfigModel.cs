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

static class ProcessHelper
{
    public static void KillAllNamedProcess(string processName)
    {
        foreach (var process in Process.GetProcessesByName(processName))
        {
            if (Process.GetCurrentProcess().Id != process.Id)
            {
                process.Kill();
            }
        }
    }
}

static class NetworkHelper
{
    public static int GetAvailablePort(int startingPort)
    {
        if (startingPort > ushort.MaxValue) throw new ArgumentException($"Can't be greater than {ushort.MaxValue}", nameof(startingPort));
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

        var connectionsEndpoints  = ipGlobalProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint);
        var tcpListenersEndpoints = ipGlobalProperties.GetActiveTcpListeners();
        var udpListenersEndpoints = ipGlobalProperties.GetActiveUdpListeners();
        var portsInUse = connectionsEndpoints.Concat(tcpListenersEndpoints)
            .Concat(udpListenersEndpoints)
            .Select(e => e.Port);

        return Enumerable.Range(startingPort, ushort.MaxValue - startingPort + 1).Except(portsInUse).FirstOrDefault();
    }
}

static class ConsoleWindowUtility
{
    public static void HideConsoleWindow()
    {
        const int SW_HIDE = 0;

        var handle = GetConsoleWindow();

        ShowWindow(handle, SW_HIDE);
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}