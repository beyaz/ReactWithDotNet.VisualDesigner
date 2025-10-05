using System.IO;
using System.Text;

namespace Toolbox;

static class FileSystem
{
    public static async Task<Result<string[]>> ReadAllLines(string filePath)
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

    public static async Task<Result<string>> ReadAllText(string filePath)
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

    public static async Task<Result<Unit>> Save(FileModel file)
    {
        try
        {
            var fileInfo = new FileInfo(file.Path);
            if (fileInfo.IsReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }

            Encoding encoding;

            if (fileInfo.Exists)
            {
                // Dosyayı açıp encoding tespit et
                using (var reader = new StreamReader(file.Path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
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
            await using var writer = new StreamWriter(file.Path, false, encoding);

            await writer.WriteAsync(file.Content);
        }
        catch (Exception exception)
        {
            return exception;
        }

        return Unit.Value;
    }
}

public sealed record FileModel
{
    public required string Content { get; init; }

    public required string Path { get; init; }
}