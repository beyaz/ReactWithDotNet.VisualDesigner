using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class NextJsExportTest
{
    [TestMethod]
    public async Task ExportAll()
    {
        (await NextJs_with_Tailwind.ExportAll(1)).Success.ShouldBeTrue();
    }

    // [TestMethod]
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
            var result = TryParseProperty(model.Properties[i].Trim());
            if (result.HasValue)
            {
                if (IsConnectedValue(result.Value.Value))
                {
                    model = model with { Properties = model.Properties.SetItem(i, result.Value.Name + ": " + ClearConnectedValue(result.Value.Value)) };
                }
            }
        }

        for (var i = 0; i < model.Children.Count; i++)
        {
            model = model with
            {
                Children = model.Children.SetItem(i, Fix(model.Children[i]))
            };
        }

        return model;
    }
}