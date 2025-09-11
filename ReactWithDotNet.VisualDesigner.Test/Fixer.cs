namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public class Fixer
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

        for (var i = 0; i < styles.Count; i++)
        {
            var text = styles[i];
            var style = ParseStyleAttribute(text);


            {
                var name = style.Name;

                var map = new Dictionary<string, string>
                {
                    ["p"] = "padding",
                    ["pt"] = "padding-top",
                    ["pb"] = "padding-bottom",
                    ["pl"] = "padding-left",
                    ["pr"] = "padding-right",
                    
                    ["m"]  = "margin",
                    ["mt"] = "margin-top",
                    ["mb"] = "margin-bottom",
                    ["ml"] = "margin-left",
                    ["mr"] = "margin-right",
                    
                    ["w"] = "width",
                    ["h"] = "height"
                };
                
               
                
                if (name.In(["width", "height", "font-size", "gap", "border-radius"]))
                {
                    if (double.TryParse(style.Value, out _))
                    {
                        styles = styles.SetItem(i, $"{name}: {style.Value.Trim()}px");
                        continue;
                    }
                }
            }
            

            


            if (style.Name == "px")
            {
                if (double.TryParse(style.Value, out _))
                {
                    styles = styles.SetItem(i, $"padding-left: {style.Value.Trim()}px");
                    styles = styles.Insert(i + 1, $"padding-right: {style.Value.Trim()}px");
                    continue;
                }
            }

            if (style.Name == "py")
            {
                if (double.TryParse(style.Value, out _))
                {
                    styles = styles.SetItem(i, $"padding-top: {style.Value.Trim()}px");
                    styles = styles.Insert(i + 1, $"padding-bottom: {style.Value.Trim()}px");
                    continue;
                }
            }

            if (double.TryParse(style.Value, out _))
            {
                ;
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