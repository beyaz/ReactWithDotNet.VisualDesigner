using System.Diagnostics;

namespace ReactWithDotNet.VisualDesigner.Toolbox;

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