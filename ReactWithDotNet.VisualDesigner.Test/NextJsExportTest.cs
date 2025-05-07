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
            if (component.Name == "HggImage")
            {
                continue;
            }
            
            var model = component.RootElementAsJson.AsVisualElementModel();

            for (var i = 0; i < model.Properties.Count; i++)
            {
                var result = TryParsePropertyValue(model.Properties[i]);
                if (result.HasValue)
                {
                    var name = result.Name;
                    var value = result.Value;

                    if (IsStringValue(value) || IsConnectedValue(value))
                    {
                        continue;
                    }

                    value.ToString();

                }
            }
        }
    }
}