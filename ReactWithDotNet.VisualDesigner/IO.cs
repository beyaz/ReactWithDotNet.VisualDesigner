using System.IO;
using System.Text;

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

            Encoding encoding;

            if (fileInfo.Exists)
            {
                // Dosyayı açıp encoding tespit et
                using (var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    // İlk okumayı yapmazsak CurrentEncoding default (UTF8) kalabilir
                    var buffer = new char[1];
                    await reader.ReadAsync(buffer, 0, 1);
                    encoding = reader.CurrentEncoding;
                }
            }
            else
            {
                // Dosya yoksa UTF-8 kullan
                encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            }

            // Dosyaya mevcut encoding ile yaz
            await using var writer = new StreamWriter(filePath, false, encoding);

            await writer.WriteAsync(fileContent);
        }
        catch (Exception exception)
        {
            return exception;
        }

        return Success;
    }
}