namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public  class Fixer
{
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
        var styles = model.Styles;
        
        for (int i = 0; i < styles.Count; i++)
        {
            var text = styles[i];
            var style = ParseStyleAttribute(text);
            if (style.Name == "width")
            {
                if (double.TryParse(style.Value, out _))
                {
                    styles = styles.SetItem(i, $"width: {style.Value.Trim()}px");
                }
            }
        }

        var children = model.Children;
        
        for (var i = 0; i < children.Count; i++)
        {
            children = children.SetItem(i, Fix(children[i]));
        }

        return model with { Styles = styles, Children = children };
    }
}