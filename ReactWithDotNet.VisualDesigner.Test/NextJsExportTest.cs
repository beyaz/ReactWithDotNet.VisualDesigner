using FluentAssertions;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class NextJsExportTest
{
    [TestMethod]
    public async Task ExportAll()
    {
        (await NextJs_with_Tailwind.ExportAll(1)).Success.Should().BeTrue();
    }

    [TestMethod]
    public async Task FixAll()
    {
        foreach (var component in await Store.GetAllComponentsInProject(1))
        {
            var visualElementModel = DeserializeFromYaml<VisualElementModel>(component.RootElementAsYaml);

            await Store.Update(component with
            {
                RootElementAsYaml = SerializeToYaml(Fix(visualElementModel))
            });
        }
    }

    static VisualElementModel Fix(VisualElementModel model)
    {
        for (var i = 0; i < model.Properties.Count; i++)
        {
            var result = TryParseProperty(model.Properties[i]);
            if (result.HasValue)
            {
                var name = result.Value.Name;

                if (name == "--text")
                {
                    model.Properties[i] = "d" + model.Properties[i].Trim().RemoveFromStart("-");
                }
            }
        }

        foreach (var child in model.Children)
        {
            Fix(child);
        }

        return model;
    }
}