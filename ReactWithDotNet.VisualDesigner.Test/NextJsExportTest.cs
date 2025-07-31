using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class NextJsExportTest
{
    [TestMethod]
    public async Task ExportAll()
    {
        (await TsxExporter.ExportAll(1)).Success.ShouldBeTrue();
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
        if (model.Tag == "a")
        {
            model = model with { Tag = "Link" };
            
            var hrefIndex = model.Properties.ToList().FindIndex(x => x.Contains("href:"));
            foreach (var tuple in TryParseProperty(model.Properties[hrefIndex]))
            {
                if (tuple.Value[0] == '/' || tuple.Value[0] == '#')
                {
                    model = model with { Properties = model.Properties.SetItem(hrefIndex, "href: "+'"'+tuple.Value + '"') };
                }
                else
                {
                    ;
                }
            }
            
            return model;
        }

        if (model.Tag == "img")
        {
            model = model with { Tag = "Image" };

            if (model.Properties.Any(x => x.Contains("width:")) is false &&
                model.Properties.Any(x => x.Contains("height:")) is false)
            {
                model = model with { Properties = model.Properties.Add("fill: true") };
            }

            var srcIndex = model.Properties.ToList().FindIndex(x => x.Contains("src:"));
            foreach (var tuple in TryParseProperty(model.Properties[srcIndex]))
            {
                if (tuple.Value.Contains("/", StringComparison.OrdinalIgnoreCase) && tuple.Value[0] != '"')
                {
                    model = model with { Properties = model.Properties.SetItem(srcIndex, "src: " + '"' + tuple.Value + '"') };
                }
            }

            return model;
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