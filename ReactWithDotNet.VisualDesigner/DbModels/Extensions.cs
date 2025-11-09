using System.IO;

namespace ReactWithDotNet.VisualDesigner.DbModels;

public static class Extensions
{
    public static string GetNameWithExportFilePath(this ComponentEntity componentEntity)
    {
        var designLocation = componentEntity.Config.DesignLocation;

        var name = componentEntity.Config.Name;

        if (Path.GetFileNameWithoutExtension(designLocation) == "{name}")
        {
            return designLocation.Replace("{name}",name);
        }

        return $"{designLocation} > {name}";
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