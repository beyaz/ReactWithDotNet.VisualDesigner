namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public class Fixer
{
    const int ProjectId = 1;

    [TestMethod]
    public async Task FixAll()
    {
        Configuration.Extensions.Config = Configuration.Extensions.ReadConfig().Value;
        
        foreach (var component in await Store.GetAllComponentsInProject(ProjectId))
        {
            var config = DeserializeFromYaml<ComponentConfig>(component.ConfigAsYaml);

            await Store.Update(component with
            {
                ConfigAsYaml = SerializeToYaml(Fix(config))
            });
        }

    }

    static ComponentConfig Fix(ComponentConfig config)
    {
        if (config.ExportFilePath.StartsWith("/src/"))
        {
            config = config with
            {
                DesignLocation = config.ExportFilePath.RemoveFromStart("/src/"),
                
                OutputFilePath = config.ExportFilePath
            };
            
            config = config with
            {
                ExportFilePath = null,
                
                OutputFilePath = "{projectDirectory}/src/{designLocation}"
            };
        }
        
        return config;
    }
}