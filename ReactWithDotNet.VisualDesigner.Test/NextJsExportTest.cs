using static ReactWithDotNet.VisualDesigner.Exporters.NextJs_with_Tailwind;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class NextJsExportTest
{
    [TestMethod]
    public async Task ExportAll()
    {
        // await NextJs_with_Tailwind.ExportAll(1);
        
        var components = await GetAllComponentsInProject(1);

        foreach (var component in components)
        {
            await Export(component.AsExportInput());
        }
    }
}