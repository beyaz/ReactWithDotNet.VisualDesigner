namespace ReactWithDotNet.VisualDesigner.CssDomain;

public static partial class CssHelper
{
    public static Maybe<DesignerStyleItem> TryCreateDesignerStyleItemFromText(ProjectConfig project, string designerStyleItem)
    {
        var result = CreateDesignerStyleItemFromText(project, designerStyleItem);
        if (result.Success)
        {
            return Maybe<DesignerStyleItem>.Some(result.Value);
        }

        return None;
    }
    public static Result<DesignerStyleItem> CreateDesignerStyleItemFromText(ProjectConfig project, string designerStyleItem)
    {
        var styleAttribute = ParseStyleAttribute(designerStyleItem);

        // H t m l
        if (styleAttribute.Value is not null)
        {
            var htmlStyle = ToHtmlStyle(project, styleAttribute.Name, styleAttribute.Value);
            if (htmlStyle.HasError)
            {
                return htmlStyle.Error;
            }

            return CreateDesignerStyleItem(new()
            {
                OriginalText = designerStyleItem,

                Pseudo = styleAttribute.Pseudo,

                FinalCssItems = [ResultFrom(htmlStyle.Value)]
            });
        }

        // P r o j e c t
        foreach (var item in tryProcessByProjectConfig(project, designerStyleItem))
        {
            return ResultFrom(item);
        }

        // T a i l w i n d
        foreach (var item in TryConvertTailwindUtilityClassToHtmlStyle(project, designerStyleItem))
        {
            return ResultFrom(item);
        }

        return new ArgumentOutOfRangeException($"{designerStyleItem} is not valid.");

        static Result<DesignerStyleItem> tryProcessByProjectConfig(ProjectConfig project, string designerStyleItem)
        {
            string name, value, pseudo;
            {
                var attribute = ParseStyleAttribute(designerStyleItem);

                name   = attribute.Name;
                value  = attribute.Value;
                pseudo = attribute.Pseudo;

                designerStyleItem = name;

                if (value is not null)
                {
                    designerStyleItem += ":" + value;
                }
            }

            if (project.Styles.TryGetValue(designerStyleItem, out var cssText))
            {
                return Style.ParseCssAsDictionary(cssText).Then(styleMap => CreateDesignerStyleItem(new()
                {
                    OriginalText = designerStyleItem,

                    Pseudo = pseudo,

                    FinalCssItems = from pair in styleMap select CreateFinalCssItem(pair)
                }));
            }

            if (name == "color" && value is not null && project.Colors.TryGetValue(value, out var realColor))
            {
                return CreateDesignerStyleItem(new()
                {
                    OriginalText = designerStyleItem,

                    Pseudo = pseudo,

                    FinalCssItems = [CreateFinalCssItem("color", realColor)]
                });
            }

            return new ArgumentOutOfRangeException($"{designerStyleItem} is not a valid project style.");
        }
    }

    public static StyleAttribute ParseStyleAttribute(string nameValueCombined)
    {
        if (string.IsNullOrWhiteSpace(nameValueCombined))
        {
            return null;
        }

        string pseudo = null;

        TryReadPseudo(nameValueCombined).HasValue(x =>
        {
            pseudo = x.Pseudo;

            nameValueCombined = x.NewText;
        });

        var colonIndex = nameValueCombined.IndexOf(':');
        if (colonIndex < 0)
        {
            return new()
            {
                Name   = nameValueCombined.Trim(),
                Pseudo = pseudo
            };
        }

        var name = nameValueCombined[..colonIndex];

        var value = nameValueCombined[(colonIndex + 1)..];

        return new()
        {
            Name   = name.Trim(),
            Value  = value.Trim(),
            Pseudo = pseudo
        };
    }
}