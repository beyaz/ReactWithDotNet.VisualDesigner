using System.IO;

namespace ReactWithDotNet.VisualDesigner.DbModels;

public static class Extensions
{
    public static string GetNameWithExportFilePath(this ComponentEntity componentEntity)
    {
        var exportFilePath = componentEntity.Config.ExportFilePath;

        var name = componentEntity.Config.Name;

        if (Path.GetFileNameWithoutExtension(exportFilePath) == name)
        {
            return exportFilePath;
        }

        return $"{exportFilePath} > {name}";
    }

    extension(ComponentEntity componentEntity)
    {
        public ComponentConfig Config
        {
            get
            {
                return Cache.AccessValue
                (
                    nameof(ComponentEntity) + nameof(Config) + componentEntity.Id,
                    () => DeserializeFromYaml<ComponentConfig>(componentEntity.ConfigAsYaml)
                );
            }
        }
    }
}