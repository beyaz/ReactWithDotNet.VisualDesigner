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

        Temp.ToArray();
    }

    static List<string> Temp = [];
    
    
    static VisualElementModel Fix(VisualElementModel model)
    {
        var styles = model.Styles;

        for (var i = 0; i < styles.Count; i++)
        {
            var text = styles[i];
            var style = ParseStyleAttribute(text);

            if (style.Value is null)
            {
                continue;
            }

            {
                if (CommonCssSuggestions.Map.ContainsKey(style.Name))
                {
                    if (CommonCssSuggestions.Map[style.Name].Contains(style.Value))
                    {
                        continue;
                    }
                }
            }

            // can be numeric
            {
                
                var canBeNumeric = "z-index,opacity,flex-grow,font-weight".Split(',',StringSplitOptions.RemoveEmptyEntries);
                if (Array.IndexOf(canBeNumeric,style.Name) >= 0)
                {
                    if (double.TryParse(style.Value, out _))
                    {
                        continue;
                    }
                }
            }
            
            {
                var name = style.Name;

                var nameIsValidCssAttributeName = name.In([
                    
                    
                    
                    "font-size", 
                    
                    "gap", 
                    
                    "top", "right", "bottom", "left",
                    
                    "width",
                    "max-width", 
                    "min-width",
                    
                    "height",
                    "max-height", 
                    "min-height",
                    
                    "border-width",
                    "border-bottom-width",
                    "border-top-width",
                    "border-lef-width",
                    "border-right-width",
                    
                    "border-radius",
                    "border-top-left-radius",
                    "border-top-right-radius",
                    "border-bottom-left-radius",
                    "border-bottom-right-radius",
                    
                    "inset",
                    
                    
                    "padding",
                    "padding-top",
                    "padding-bottom",
                    "padding-left",
                    "padding-right",
                    
                    "margin",
                    "margin-top",
                    "margin-bottom",
                    "margin-left",
                    "margin-right",
                    
                    "flex",
                    "flex-grow",
                    "flex-shrink"
                    

                ]);
                if (!nameIsValidCssAttributeName)
                {
                    var map = new Dictionary<string, string>
                    {
                        ["p"]  = "padding",
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

                    if (map.ContainsKey(name))
                    {
                        name = map[name];
                        
                        nameIsValidCssAttributeName = true;
                    }
                    
                }
               
                
                if (nameIsValidCssAttributeName)
                {
                    if (double.TryParse(style.Value, out _))
                    {
                        styles = styles.SetItem(i, $"{name}: {style.Value.Trim()}px");
                        continue;
                    }
                    
                    if (double.TryParse(style.Value.RemoveFromEnd("%"), out _))
                    {
                        styles = styles.SetItem(i, $"{name}: {style.Value.Trim()}%");
                        continue;
                    }
                    
                    if (double.TryParse(style.Value.RemoveFromEnd("px"), out _))
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
            
            if (style.Name == "size")
            {
                if (double.TryParse(style.Value, out _))
                {
                    styles = styles.SetItem(i, $"width: {style.Value.Trim()}px");
                    styles = styles.Insert(i + 1, $"height: {style.Value.Trim()}px");
                    continue;
                }
            }

            if (double.TryParse(style.Value, out _))
            {
                ;
            }

            if (Temp.Contains(style.Name))
            {
                continue;
            }

            Temp.Add(style.Name +" : "+ style.Value);
        }

        var children = model.Children;

        for (var i = 0; i < children.Count; i++)
        {
            children = children.SetItem(i, Fix(children[i]));
        }

        return model with { Styles = styles, Children = children };
    }
}