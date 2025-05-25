using System.Diagnostics;

namespace ReactWithDotNet.VisualDesigner.Toolbox;

static class IdeBridge
{
    public static void OpenWebStormEditor(string filePath, int lineNumber)
    {
        var response = FindRunningProcessExecutablePath("webstorm64");

        var webstormPath = response.FilePath;

        var startInfo = new ProcessStartInfo
        {
            FileName        = webstormPath,
            Arguments       = $"--line {lineNumber} \"{filePath}\"",
            UseShellExecute = false,
            CreateNoWindow  = true
        };

        Process.Start(startInfo);
    }

    public static (bool fail, Exception exception) OpenWebStormEditor(string webstormExeFilePath, string filePath, int lineNumber)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName        = webstormExeFilePath,
                Arguments       = $"--line {lineNumber} \"{filePath}\"",
                UseShellExecute = false,
                CreateNoWindow  = true
            };

            Process.Start(startInfo);

            return default;
        }
        catch (Exception exception)
        {
            return (fail: true, exception);
        }
    }

    static (string FilePath, bool HasFail, Exception Exception) FindRunningProcessExecutablePath(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        if (processes.Length > 0)
        {
            try
            {
                var filePath = processes[0].MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    return (filePath, false, null);
                }

                return (null, true, new("FilePathIsEmpty"));
            }
            catch (Exception exception)
            {
                return (null, true, exception);
            }
        }

        return (null, true, new("ProcessNotFound"));
    }
}