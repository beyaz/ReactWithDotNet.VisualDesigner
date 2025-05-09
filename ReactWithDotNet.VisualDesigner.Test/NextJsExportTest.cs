using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class NextJsExportTest
{
    [TestMethod]
    public async Task ExportAll()
    {
        await NextJs_with_Tailwind.ExportAll(1);
    }

    [TestMethod]
    public async Task VisitElements()
    {
        var components = await GetAllComponentsInProject(1);

        foreach (var component in components)
        {
            if (component.Name == "HggImage")
            {
                continue;
            }

            var root = component.RootElementAsJson.AsVisualElementModel();

            visitProperties(root);
        }

        return;

        static void visitProperties(VisualElementModel model)
        {
            for (var i = 0; i < model.Properties.Count; i++)
            {
                var result = TryParsePropertyValue(model.Properties[i]);
                if (result.HasValue)
                {
                    var name = result.Name;
                    var value = result.Value;

                    if (name == "-bind")
                    {
                        if (model.Text.HasNoValue())
                        {
                            ;
                        }
                    }
                    

                    value.ToString();
                }
            }

            foreach (var child in model.Children)
            {
                visitProperties(child);
            }
        }
    }
}