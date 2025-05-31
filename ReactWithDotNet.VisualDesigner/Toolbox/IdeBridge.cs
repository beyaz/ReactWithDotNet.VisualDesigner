using System.Diagnostics;
using System.IO;

namespace ReactWithDotNet.VisualDesigner.Toolbox;

static class IdeBridge
{
    public static Exception OpenEditor(string filePath, int lineNumber)
    {
        // try webstorm
        {
            var response = FindRunningProcessExecutablePath("webstorm64");
            if (response.Success)
            {
                try
                {
                    var exeFilePath = response.FilePath;

                    var startInfo = new ProcessStartInfo
                    {
                        FileName        = exeFilePath,
                        Arguments       = $"--line {lineNumber} \"{filePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow  = true
                    };

                    Process.Start(startInfo);

                    return null;
                }
                catch (Exception exception)
                {
                    return exception;
                }
            }
        }

        // try vscode
        {
            var response = FindRunningProcessExecutablePath("Code");
            if (response.Success)
            {
                try
                {
                    var exeFilePath = response.FilePath;

                    var startInfo = new ProcessStartInfo
                    {
                        FileName        = exeFilePath,
                        Arguments       = $"--goto \"{filePath}:{lineNumber}\"",
                        UseShellExecute = true,
                        CreateNoWindow  = true
                    };

                    Process.Start(startInfo);

                    return null;
                }
                catch (Exception exception)
                {
                    return exception;
                }
            }
        }

        // try visual studio
        {
            var vsOpenFileAtLineExeFilePath = Path.Combine(Path.GetDirectoryName(typeof(IdeBridge).Assembly.Location) ?? string.Empty, "VsOpenFileAtLine.exe");

            if (File.Exists(vsOpenFileAtLineExeFilePath))
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName        = vsOpenFileAtLineExeFilePath,
                        Arguments       = $"{filePath} {lineNumber}",
                        UseShellExecute = false,
                        CreateNoWindow  = true
                    };

                    Process.Start(startInfo);

                    return null;
                }
                catch (Exception exception)
                {
                    return exception;
                }
            }
        }

        return new("EditorNotFound");
    }

    static (string FilePath, bool Success, Exception Exception) FindRunningProcessExecutablePath(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        if (processes.Length > 0)
        {
            try
            {
                var filePath = processes[0].MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    return (filePath, true, null);
                }

                return (null, false, new("FilePathIsEmpty"));
            }
            catch (Exception exception)
            {
                return (null, false, exception);
            }
        }

        return (null, false, new("ProcessNotFound"));
    }
}