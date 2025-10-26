﻿using System.IO;

namespace ReactWithDotNet.VisualDesigner;

static class ComponentConfigReservedName
{
    public static readonly string ExportFilePath = "ExportFilePath";
    
    public static readonly string Name = "Name";
}

static partial class Extensions
{
    public static IReadOnlyDictionary<string, string> GetConfig(this ComponentEntity componentEntity)
    {
        return DeserializeFromYaml<Dictionary<string, string>>(componentEntity.ConfigAsYaml);
    }
    
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