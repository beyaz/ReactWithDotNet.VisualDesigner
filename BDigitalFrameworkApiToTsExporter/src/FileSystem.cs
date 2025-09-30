using System.Text;

namespace BDigitalFrameworkApiToTsExporter;

record FileModel(string Path, string Content);

static class FileSystem
{
    public static Result<string> ReadAllText(string filePath)
    {
        try
        {
            return File.ReadAllText(filePath);
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