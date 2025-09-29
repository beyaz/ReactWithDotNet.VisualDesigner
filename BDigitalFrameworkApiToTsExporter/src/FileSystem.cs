using System.Text;

namespace b_digital_framework_type_exporter;

static class FileSystem
{
    public static (string? fileContent, Exception? exception) ReadAllText(string filePath)
    {
        try
        {
            return (File.ReadAllText(filePath), null);
        }
        catch (Exception exception)
        {
            return (null, exception);
        }
    }

    public static Exception? WriteAllText(string filePath, string fileContent)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Exists)
            {
                fileInfo.IsReadOnly = false;

                TfsHelper.CheckoutFileFromTfs(filePath);
            }

            File.WriteAllText(filePath, fileContent, Encoding.UTF8);
        }
        catch (Exception exception)
        {
            return exception;
        }

        return null;
    }
}