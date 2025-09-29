using System.Diagnostics;

namespace b_digital_framework_type_exporter;

sealed class CheckoutFileFromTfsResponse
{
    public string? Error { get; init; }

    public string? Output { get; init; }

    public bool Success { get; init; }

    public bool TfExeNotFound { get; init; }

    public bool VsInstallationPathNotFound { get; init; }

    public bool VsWhereExeNotFound { get; init; }
}

static class TfsHelper
{
    public static CheckoutFileFromTfsResponse CheckoutFileFromTfs(string filePath)
    {
        var vsWherePath = Path.Combine
            (
             Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
             @"Microsoft Visual Studio\Installer\vswhere.exe"
            );

        if (!File.Exists(vsWherePath))
        {
            return new() { VsWhereExeNotFound = true };
        }

        // Run vswhere to get the installation path
        var vsWhereProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName               = vsWherePath,
                Arguments              = "-latest -property installationPath",
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            }
        };

        vsWhereProcess.Start();
        var vsInstallPath = vsWhereProcess.StandardOutput.ReadLine()?.Trim();
        vsWhereProcess.WaitForExit();

        if (string.IsNullOrEmpty(vsInstallPath))
        {
            return new() { VsInstallationPathNotFound = true };
        }

        var tfExePath = Path.Combine
            (
             vsInstallPath,
             @"Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\tf.exe"
            );

        if (!File.Exists(tfExePath))
        {
            return new() { TfExeNotFound = true };
        }

        // Run tf checkout
        var tfProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName               = tfExePath,
                Arguments              = $"checkout \"{filePath}\"",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            }
        };

        tfProcess.Start();
        var output = tfProcess.StandardOutput.ReadToEnd();
        var error = tfProcess.StandardError.ReadToEnd();
        tfProcess.WaitForExit();

        if (!string.IsNullOrEmpty(error))
        {
            return new() { Error = error };
        }

        return new()
        {
            Success = true,
            Output  = output
        };
    }
}