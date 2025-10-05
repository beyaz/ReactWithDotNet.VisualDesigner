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

    public static Result<Unit> Save(FileModel file)
    {
        try
        {
            var fileInfo = new FileInfo(file.Path);

            if (fileInfo.Exists)
            {
                fileInfo.IsReadOnly = false;
                TfsHelper.CheckoutFileFromTfs(file.Path);
            }

            File.WriteAllText(file.Path, file.Content, Encoding.UTF8);

            return Unit.Value;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public static Result<Unit> SaveAll(IEnumerable<FileModel> files)
    {
        foreach (var file in files)
        {
            var result = Save(file);
            if (result.HasError)
            {
                return result;
            }
        }

        return Unit.Value;
    }
}