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

    public static  Task<Result<Unit>> SaveAll(IEnumerable<FileModel> files)
    {
        return 
            from file in files
            from unit in Save(file)
            select unit;
    }
}