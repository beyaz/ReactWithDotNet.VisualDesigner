using System.IO;

namespace ReactWithDotNet.VisualDesigner;

static class IO
{
    public static async Task<Result<string>> TryReadFile(string filePath)
    {
        try
        {
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public static async Task<Result<string[]>> TryReadFileAllLines(string filePath)
    {
        try
        {
            return await File.ReadAllLinesAsync(filePath);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public static async Task<Result> TryWriteToFile(string filePath, string fileContent)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.IsReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }
            
            await File.WriteAllTextAsync(filePath, fileContent);
        }
        catch (Exception exception)
        {
            return exception;
        }

        return Success;
    }
}