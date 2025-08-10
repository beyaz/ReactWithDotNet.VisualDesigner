using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;

namespace ReactWithDotNet.VisualDesigner.Toolbox;

static class Prettier
{
    static readonly object _lock = new();

    static readonly HttpClient HttpClient = new();

    public static async Task<Result<string>> FormatCode(string code)
    {
        await StartServerIfNeeded();

        var options = new JsonSerializerOptions();

        var requestObject = new { code = code };

        var jsonPayload = JsonSerializer.Serialize(requestObject, options);

        var jsonContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var httpResponseMessage = await HttpClient.PostAsync("http://localhost:5009/format", jsonContent);

        var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

        var response = JsonSerializer.Deserialize<Response>(responseContent, options);

        if (response.error is not null)
        {
            return new FormatException(string.Join(Environment.NewLine + Environment.NewLine,[code, response.error, response.details]));
        }

        return response.formattedCode.TrimEnd().RemoveFromEnd(";");
    }

    public static void Register(WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() => { StartServerAsync(); });
        app.Lifetime.ApplicationStopping.Register(StopServer);
    }

    static Task StartServerAsync()
    {
        if (!Directory.Exists(data.ProjectPath))
        {
            throw new DirectoryNotFoundException($"Prettier: {data.ProjectPath}");
        }

        var processInfo = new ProcessStartInfo
        {
            FileName               = "cmd.exe",
            Arguments              = $"/C cd /D \"{data.ProjectPath}\" & npm start",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        data.NodeProcess = new() { StartInfo = processInfo };

        data.NodeProcess.Start();

        return Task.CompletedTask;
    }

    static async Task StartServerIfNeeded()
    {
        if (data.NodeProcess != null && !data.NodeProcess.HasExited)
        {
            return;
        }

        lock (_lock)
        {
            if (data.StartTask == null || data.StartTask.IsCompleted)
            {
                data.StartTask = StartServerAsync();
            }
        }

        await data.StartTask;
    }

    static void StopServer()
    {
        if (data.NodeProcess != null && !data.NodeProcess.HasExited)
        {
            try
            {
                data.NodeProcess.Kill(true);
                data.NodeProcess.WaitForExit(5000);
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    sealed record data
    {
        public static Process NodeProcess { get; set; }

        public static string ProjectPath => Path.Combine(Path.GetDirectoryName(typeof(Prettier).Assembly.Location) ?? string.Empty, "HelperApps", "TsxFormatter");

        public static Task StartTask { get; set; }
    }

    record Response
    {
        public string details { get; init; }

        public string error { get; init; }
        
        public string formattedCode { get; init; }
    }
}