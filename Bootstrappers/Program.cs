using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace ReactWithDotNet.VisualDesigner.Bootstrapper;

static class Program
{
    public static async Task Main()
    {
        try
        {
            await Run(CalculateConfig());
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);

            Console.Read();
        }
    }

    static Config CalculateConfig()
    {
        return new Config
        {
            AppExeFilePath               = (string)AppContext.GetData(nameof(Config.AppExeFilePath)),
            DbConnectionString           = (string)AppContext.GetData(nameof(Config.DbConnectionString)),
            InstallationFolder           = (string)AppContext.GetData(nameof(Config.InstallationFolder)),
            KillAllNamedProcess          = (string)AppContext.GetData(nameof(Config.KillAllNamedProcess)),
            LocalZipFilePath             = (string)AppContext.GetData(nameof(Config.LocalZipFilePath)),
            QueryGetFileContent          = (string)AppContext.GetData(nameof(Config.QueryGetFileContent)),
            QueryGetLastModificationDate = (string)AppContext.GetData(nameof(Config.QueryGetLastModificationDate)),
        };
    }

    static async Task Run(Config config)
    {
        Action<string> trace = Console.WriteLine;

        // K i l l   n a m e d   p r o c e s s
        {
            foreach (var processName in config.KillAllNamedProcess.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                trace($"Killing process: {processName}");

                foreach (var process in Process.GetProcessesByName(processName))
                {
                    if (Process.GetCurrentProcess().Id != process.Id)
                    {
                        process.Kill(true);
                    }
                }
            }

            // K i l l   r e l a t e d   n o d e j s    p r o c e s s
            if (Directory.Exists(config.InstallationFolder))
            {
                foreach (var file in Directory.GetFiles(config.InstallationFolder, "node.js.process.id.*"))
                {
                    var processId = Path.GetFileNameWithoutExtension(file).RemoveFromStart("node.js.process.id.");

                    var processIdAsNumber = int.Parse(processId);

                    foreach (var process in Process.GetProcesses())
                    {
                        if (processIdAsNumber == process.Id)
                        {
                            process.Kill(true);
                        }
                    }

                    File.Delete(file);
                }
            }
        }

        // D o w n l o a d   l a t e s t   v e r s i o n
        {
            trace("Checking version...");

            var localZipFileCreationDate = File.Exists(config.LocalZipFilePath) ? File.GetCreationTime(config.LocalZipFilePath) : DateTime.MinValue;

            await using var connection = new SqlConnection(config.DbConnectionString);

            connection.Open();

            var needToUpdate = true;

            // C o m p a r e   d a t e s
            {
                await using var command = new SqlCommand(config.QueryGetLastModificationDate, connection);

                var dbValue = await command.ExecuteScalarAsync();

                var lastModificationDate = Convert.ToDateTime(dbValue);

                if (localZipFileCreationDate > lastModificationDate)
                {
                    trace("Already using latest version.");

                    needToUpdate = false;
                }
            }

            // G e t   l a t e s t   v e r s i o n
            if (needToUpdate)
            {
                trace("Deleting old version");

                if (config.DeleteOldVersion == "true" && Directory.Exists(config.InstallationFolder))
                {
                    Directory.Delete(config.InstallationFolder, true);
                }

                trace("Getting latest version.");

                await using var command = new SqlCommand(config.QueryGetFileContent, connection);

                var bytes = (byte[])await command.ExecuteScalarAsync();
                if (bytes is null)
                {
                    throw new Exception("File content not fetched. Zero bytes fetched.");
                }

                // S a v e   a s   l o c a l   f i l e
                {
                    trace($"Saving local file: {config.LocalZipFilePath}");

                    var directoryName = Path.GetDirectoryName(config.LocalZipFilePath);

                    if (directoryName != null && !Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    await File.WriteAllBytesAsync(config.LocalZipFilePath, bytes);
                }

                // E x t r a c t   l o c a l   z i p   f i l e
                {
                    trace("Started to extract zip file...");

                    ZipFile.ExtractToDirectory(config.LocalZipFilePath, config.InstallationFolder, true);

                    trace("Zip file extracted");
                }
            }

            await connection.CloseAsync();
        }

        // S t a r t   a p p l i c a t i o n
        {
            trace($"Starting application: {config.AppExeFilePath}");

            var processStartInfo = new ProcessStartInfo(config.AppExeFilePath)
            {
                WorkingDirectory = Path.GetDirectoryName(config.AppExeFilePath) ?? Environment.CurrentDirectory,

                CreateNoWindow = true,

                UseShellExecute = false
            };

            Process.Start(processStartInfo);

            trace("Started.");
        }
    }

    sealed class Config
    {
        public string AppExeFilePath { get; init; }

        public string DbConnectionString { get; init; }

        public string DeleteOldVersion { get; init; }

        public string InstallationFolder { get; init; }

        public string KillAllNamedProcess { get; init; }

        public string LocalZipFilePath { get; init; }

        public string QueryGetFileContent { get; init; }

        public string QueryGetLastModificationDate { get; init; }
    }
}

static class Extensions
{
    /// <summary>
    ///     Removes value from start of str
    /// </summary>
    public static string RemoveFromStart(this string data, string value)
    {
        return RemoveFromStart(data, value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Removes value from start of str
    /// </summary>
    public static string RemoveFromStart(this string data, string value, StringComparison comparison)
    {
        if (data == null)
        {
            return null;
        }

        if (data.StartsWith(value, comparison))
        {
            return data.Substring(value.Length, data.Length - value.Length);
        }

        return data;
    }
}