using System.IO;

namespace ReactWithDotNet.VisualDesigner;

static class ComponentConfigReservedName
{
    public static readonly string ExportFilePath = "exportFilePath";
    
    public static readonly string Name = "name";
}

static class ComponentEntityExtensions
{
    
    
    public static string GetNameWithExportFilePath(this ComponentEntity componentEntity)
    {
        var exportFilePath = GetExportFilePath(componentEntity);
        
        var name = GetName(componentEntity);

        if (Path.GetFileNameWithoutExtension(exportFilePath) == name)
        {
            return exportFilePath;
        }
        
        
        return $"{exportFilePath} > {name}";
    }
    
    public static Maybe<ComponentEntity> TryFindComponentByComponentNameWithExportFilePath(int projectId, string componentNameWithExportFilePath)
    {
        if (componentNameWithExportFilePath.HasNoValue())
        {
            return None;
        }
        
        if (componentNameWithExportFilePath.Contains(" > ",StringComparison.OrdinalIgnoreCase))
        {
            var index = componentNameWithExportFilePath.IndexOf(" > ", StringComparison.OrdinalIgnoreCase);

            var exportFilePath = componentNameWithExportFilePath.Substring(0, index);
            var componentName = componentNameWithExportFilePath.Substring(index + 3, componentNameWithExportFilePath.Length -index -3);

            var record = GetAllComponentsInProjectFromCache(projectId).FirstOrDefault(x => x.GetExportFilePath() == exportFilePath && x.GetName() == componentName);
            if (record is not null)
            {
                return record;
            }
        }
        
        if (componentNameWithExportFilePath.Contains("/",StringComparison.OrdinalIgnoreCase))
        {
            var exportFilePath = componentNameWithExportFilePath;
            var componentName = Path.GetFileNameWithoutExtension(componentNameWithExportFilePath);

            var record = GetAllComponentsInProjectFromCache(projectId).FirstOrDefault(x => x.GetExportFilePath() == exportFilePath && x.GetName() == componentName);
            if (record is not null)
            {
                return record;
            }
        }

        return None;
    }
    
    public static string GetExportFilePath(this ComponentEntity componentEntity)
    {
        foreach (var name in componentEntity.TryGetExportFilePath())
        {
            return name;
        }

        throw new InvalidOperationException("exportFilePath not found");
    }

    public static string GetName(this ComponentEntity componentEntity)
    {
        foreach (var name in componentEntity.TryGetName())
        {
            return name;
        }

        throw new InvalidOperationException("name not found");
    }

    public static Maybe<string> TryGetComponentExportFilePath(this IReadOnlyDictionary<string, string> componentConfig)
    {
        if (componentConfig.TryGetValue(ComponentConfigReservedName.ExportFilePath, out var value))
        {
            return value;
        }

        return None;
    }

    public static Maybe<string> TryGetComponentName(this IReadOnlyDictionary<string, string> componentConfig)
    {
        if (componentConfig.TryGetValue(ComponentConfigReservedName.Name, out var value))
        {
            return value;
        }

        return None;
    }

    public static Maybe<string> TryGetExportFilePath(this ComponentEntity componentEntity)
    {
        if (DeserializeFromYaml<Dictionary<string, string>>(componentEntity.ConfigAsYaml).TryGetValue(ComponentConfigReservedName.ExportFilePath, out var value))
        {
            return value;
        }

        return None;
    }

    public static Maybe<string> TryGetName(this ComponentEntity componentEntity)
    {
        if (DeserializeFromYaml<Dictionary<string, string>>(componentEntity.ConfigAsYaml).TryGetValue(ComponentConfigReservedName.Name, out var value))
        {
            return value;
        }

        return None;
    }
}