namespace ReactWithDotNet.VisualDesigner;

static class ComponentEntityExtensions
{
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
        if (componentConfig.TryGetValue("exportFilePath", out var value))
        {
            return value;
        }

        return None;
    }

    public static Maybe<string> TryGetComponentName(this IReadOnlyDictionary<string, string> componentConfig)
    {
        if (componentConfig.TryGetValue("name", out var value))
        {
            return value;
        }

        return None;
    }

    public static Maybe<string> TryGetExportFilePath(this ComponentEntity componentEntity)
    {
        if (DeserializeFromYaml<Dictionary<string, string>>(componentEntity.ConfigAsYaml).TryGetValue("exportFilePath", out var value))
        {
            return value;
        }

        return None;
    }

    public static Maybe<string> TryGetName(this ComponentEntity componentEntity)
    {
        if (DeserializeFromYaml<Dictionary<string, string>>(componentEntity.ConfigAsYaml).TryGetValue("name", out var value))
        {
            return value;
        }

        return None;
    }
}