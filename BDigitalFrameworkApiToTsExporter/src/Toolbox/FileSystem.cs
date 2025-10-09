using System.Text;

namespace Toolbox;

public sealed record FileModel
{
    public required string Content { get; init; }

    public required string Path { get; init; }
}

static class FileSystem
{
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

            if (fileInfo.Exists)
            {
                // check already same
                var fileContentInDirectory = await File.ReadAllTextAsync(file.Path);
                
                if (IsEqualsIgnoreWhitespace(fileContentInDirectory, file.Content))
                {
                    return Unit.Value;
                }
                    
                
                fileInfo.IsReadOnly = false;
                TfsHelper.CheckoutFileFromTfs(file.Path);
            }

            await File.WriteAllTextAsync(file.Path, file.Content, Encoding.UTF8);

            return Unit.Value;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}