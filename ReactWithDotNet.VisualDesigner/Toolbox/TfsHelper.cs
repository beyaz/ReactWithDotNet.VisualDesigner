using System.Diagnostics;
using System.IO;

namespace Toolbox;

static class TfsHelper
{
    public static Exception AddFile(string filePath)
    {
        var (tfsExePath, exception) = Find_Tfs_exe();
        if (exception is not null)
        {
            return exception;
        }

        var tfProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName               = tfsExePath,
                Arguments              = $"add \"{filePath}\"",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            }
        };

        tfProcess.Start();
        var error = tfProcess.StandardError.ReadToEnd();
        tfProcess.WaitForExit();

        if (!string.IsNullOrEmpty(error))
        {
            return new IOException(error);
        }

        return null;
    }

    public static Exception CheckoutFile(string filePath)
    {
        var (tfsExePath, exception) = Find_Tfs_exe();
        if (exception is not null)
        {
            return exception;
        }

        var tfProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName               = tfsExePath,
                Arguments              = $"checkout \"{filePath}\"",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            }
        };

        tfProcess.Start();
        var error = tfProcess.StandardError.ReadToEnd();
        tfProcess.WaitForExit();

        if (!string.IsNullOrEmpty(error))
        {
            return new IOException(error);
        }

        return null;
    }

    static (string tfsExePath, Exception exception) Find_Tfs_exe()
    {
        var vsWherePath = Path.Combine
            (
             Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
             @"Microsoft Visual Studio\Installer\vswhere.exe"
            );

        if (!File.Exists(vsWherePath))
        {
            return new(string.Empty, new FileNotFoundException(vsWherePath));
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
            return new(string.Empty, new FileNotFoundException("VsInstallationPathNotFound"));
        }

        var tfExePath = Path.Combine
            (
             vsInstallPath,
             @"Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\tf.exe"
            );

        if (!File.Exists(tfExePath))
        {
            return new(string.Empty, new FileNotFoundException(tfExePath));
        }

        return (tfExePath, null);
    }
}