namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public class Fixer
{
    const int ProjectId = 1;

    static readonly ProjectConfig Project = GetProjectConfig(ProjectId);

    static readonly List<string> Temp = [];

    [TestMethod]
    public async Task FixAll()
    {
        foreach (var component in await Store.GetAllComponentsInProject(ProjectId))
        {
            var visualElementModel = DeserializeFromYaml<VisualElementModel>(component.RootElementAsYaml);

            await Store.Update(component with
            {
                RootElementAsYaml = SerializeToYaml(Fix(visualElementModel))
            });
        }

        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        Temp.ToArray();
    }

    static VisualElementModel Fix(VisualElementModel model)
    {
        var styles = model.Styles;

        for (var i = 0; i < styles.Count; i++)
        {
            var text = styles[i];
            
            var style = ParseStyleAttribute(text);

            if (style.Name == "max-h-44")
            {
                styles = styles.SetItem(i, "max-height: 176px");
                continue;
            }
            
            if (style.Name == "min-h-44")
            {
                styles = styles.SetItem(i, "min-height: 176px");
                continue;
            }
            
            if (style.Name == "min-w-56")
            {
                styles = styles.SetItem(i, "min-width: 224px");
                continue;
            }
            
            if (style.Name == "max-w-56")
            {
                styles = styles.SetItem(i, "max-width: 224px");
                continue;
            }
            
            if (style.Name == "font-medium")
            {
                styles = styles.SetItem(i, "font-weight: 500");
                continue;
            }
            
            if (style.Name == "line-height")
            {
                if (style.Value == "24px" || style.Value == "32px")
                {
                    continue;
                }
            }

            if (style.Name == "font-family")
            {
                if (style.Value == "Host Grotesk")
                {
                    continue;
                }
            }
            
            if (style.Name == "bg")
            {
                styles = styles.SetItem(i, $"background: {style.Value}");
                continue;
            }
            
            if (style.Name == "visibility")
            {
                // todo: condition
                continue;
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
            
            if (style.Name == "px")
            {
                if (double.TryParse(style.Value, out _))
                {
                    styles = styles.SetItem(i, $"padding-left: {style.Value.Trim()}px");
                    styles = styles.Insert(i + 1, $"padding-right: {style.Value.Trim()}px");
                    continue;
                }

                if (style.Value.EndsWith("rem"))
                {
                    styles = styles.SetItem(i, $"padding-left: {style.Value.Trim()}");
                    styles = styles.Insert(i + 1, $"padding-right: {style.Value.Trim()}");
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
            
            // resolve names
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

                if (map.ContainsKey(style.Name))
                {
                    styles = styles.SetItem(i, $"{map[style.Name]}: {style.Value}");
                    continue;
                }
            }

            // common ends with px
            {
                var nameIsValidCssAttributeName = style.Name.In([
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
                ]);
                
                if (nameIsValidCssAttributeName)
                {
                    if (double.TryParse(style.Value, out _))
                    {
                        styles = styles.SetItem(i, $"{style.Name}: {style.Value.Trim()}px");
                        continue;
                    }

                    if (double.TryParse(style.Value.RemoveFromEnd("%"), out _))
                    {
                        styles = styles.SetItem(i, $"{style.Name}: {style.Value.Trim()}");
                        continue;
                    }

                    if (double.TryParse(style.Value.RemoveFromEnd("px"), out _))
                    {
                        styles = styles.SetItem(i, $"{style.Name}: {style.Value.Trim()}");
                        continue;
                    }

                    if (double.TryParse(style.Value.RemoveFromEnd("rem"), out _))
                    {
                        styles = styles.SetItem(i, $"{style.Name}: {style.Value.Trim()}");
                        continue;
                    }
                }
            }
            
            // can be numeric
            {
                var canBeNumeric = "z-index,opacity,flex,flex-shrink,flex-grow,font-weight".Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (Array.IndexOf(canBeNumeric, style.Name) >= 0)
                {
                    if (double.TryParse(style.Value, out _))
                    {
                        continue;
                    }
                }
            }
            
            if (style.Value is null)
            {
                if (Project.Styles.ContainsKey(style.Name))
                {
                    continue;
                }
            }
            
            // is  common css suggestion
            {
                if (CommonCssSuggestions.Map.ContainsKey(style.Name))
                {
                    if (CommonCssSuggestions.Map[style.Name].Contains(style.Value))
                    {
                        continue;
                    }
                }
            }
            
            {
                if (style.Name == "color" || style.Name == "background")
                {
                    if (Project.Colors.ContainsKey(style.Value))
                    {
                        continue;
                    }
                    
                    if (TryGetWebColorByName(style.Value) is not null )
                    {
                        continue;
                    }
                }
            }
          
            
            //continue;

            
            
            
            

            // todo:
            {
                if (style.Name.StartsWith("outline"))
                {
                    continue;
                }

                if (style.Name == "padding")
                {
                    continue;
                }

                if (style.Name.StartsWith("border"))
                {
                    continue;
                }

                if (style.Name == "background")
                {
                    continue;
                }

                if (style.Name == "color")
                {
                    continue;
                }

                if (style.Name == "background" && style.Value.StartsWith("rgb"))
                {
                    continue;
                }

                if (style.Name == "bg" && style.Value == "white")
                {
                    continue;
                }

                if (style.Name == "color" && style.Value == "white")
                {
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

            Temp.Add(style.Name + " : " + style.Value);
        }

        var children = model.Children;

        for (var i = 0; i < children.Count; i++)
        {
            children = children.SetItem(i, Fix(children[i]));
        }

        return model with { Styles = styles, Children = children };
    }
}