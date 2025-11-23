namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class NextJsExportTest
{
    [TestMethod]
    public async Task ExportAll()
    {
        Configuration.Extensions.Config = Configuration.Extensions.ReadConfig().Value;

        const int projectId = 1;

        var components = await Store.GetAllComponentsInProject(projectId);

        foreach (var component in components)
        {
            if (component.Config.SkipExport)
            {
                continue;
            }

            var componentScope = await GetComponentScope(component.Id, null);
            if (componentScope.HasError)
            {
                throw componentScope.Error;
            }

            var result = await TsxExporter.ExportToFileSystem(componentScope.Value);

            if (result.HasError)
            {
                throw result.Error;
            }
        }
    }
}