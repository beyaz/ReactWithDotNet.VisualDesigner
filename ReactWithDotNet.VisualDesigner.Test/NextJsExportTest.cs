using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class NextJsExportTest
{
    [TestMethod]
    public async Task ExportAll()
    {
        const int projectId = 1;

        var components = await Store.GetAllComponentsInProject(projectId);

        foreach (var component in components)
        {
            if (component.GetConfig().TryGetValue("IsExportable", out var isExportable) && isExportable.Equals("False", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var result = await TsxExporter.ExportToFileSystem(new()
            {
                ComponentId = component.Id,
                ProjectId   = component.ProjectId,
                UserName    = EnvironmentUserName
            });

            result.Success.ShouldBeTrue();
        }
    }

   
}