using System.IO;

namespace ReactWithDotNet.VisualDesigner.DbModels;

static class ComponentConfigReservedName
{
    public static readonly string ExportFilePath = "ExportFilePath";
    
    public static readonly string Name = "Name";
}

public static class Extensions
{
   

    extension(ComponentEntity componentEntity)
    {
        public ComponentConfig Config
        {
            get
            {
                return Cache.AccessValue
                (
                    nameof(ComponentEntity) +nameof(Config) + componentEntity.Id,
                    
                    () => DeserializeFromYaml<ComponentConfig>(componentEntity.ConfigAsYaml)
                );
            }
        }
    }
    
    
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
    
    
    

    

    

    

    

  
    
   
}