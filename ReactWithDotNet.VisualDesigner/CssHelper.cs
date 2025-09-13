
namespace ReactWithDotNet.VisualDesigner;

public static partial class CssHelper
{
    public static Result<DesignerStyleItem> CreateDesignerStyleItemFromText(ProjectConfig project, string designerStyleItem)
    {
        // try process from plugin
        {
            var result = tryProcessByProjectConfig(project, designerStyleItem);
            if (result.HasError)
            {
                return result.Error;
            }

            if (result.Value is not null)
            {
                return result.Value;
            }
        }

        {
            foreach (var item in TryConvertTailwindUtilityClassToHtmlStyle(project, designerStyleItem))
            {
                return item;
            }
        }

        // final calculation
        {
            string name, value, pseudo;
            {
                var attribute = ParseStyleAttribute(designerStyleItem);

                name   = attribute.Name;
                value  = attribute.Value;
                pseudo = attribute.Pseudo;
            }

            if (value is not null)
            {
                var htmlStyle = ToHtmlStyle(project, name, value);
                if (htmlStyle.HasError)
                {
                    return htmlStyle.Error;
                }

                return CreateDesignerStyleItem(pseudo, htmlStyle.Value);
            }

            return new Exception("Value is required");
        }

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
                return Style.ParseCssAsDictionary(cssText)
                    .Then(styleMap 
                              => CreateDesignerStyleItem(pseudo, ListFrom(from pair in styleMap select CreateFinalCssItem(pair)))
                );
            }

            if (name == "color" && value is not null && project.Colors.TryGetValue(value, out var realColor))
            {
                return CreateDesignerStyleItem(pseudo, CreateFinalCssItem("color", realColor));
            }

            return None;
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

    public static Result<StyleModifier> ToStyleModifier(this DesignerStyleItem designerStyleItem)
    {
        ArgumentNullException.ThrowIfNull(designerStyleItem);

        var style = new Style();

        foreach (var finalCssItem in arrangeCondition(designerStyleItem.FinalCssItems))
        {
            var exception = style.TrySet(finalCssItem.Name, finalCssItem.Value);
            if (exception is not null)
            {
                return exception;
            }
        }

        if (designerStyleItem.Pseudo is not null)
        {
            return ApplyPseudo(designerStyleItem.Pseudo, [CreateStyleModifier(x => x.Import(style))]);
        }

        return (StyleModifier)style;

        static IEnumerable<FinalCssItem> arrangeCondition(IReadOnlyList<FinalCssItem> finalCssItems)
        {

            return from finalCssItem in finalCssItems

                let parseResult = TryParseConditionalValue(finalCssItem.Value)

                select parseResult.success 
                    ? parseResult.right is not null 
                        ? CreateFinalCssItem(finalCssItem.Name, parseResult.right) 
                        : CreateFinalCssItem(finalCssItem.Name, parseResult.left)
                    : finalCssItem;


        }

        static Result<StyleModifier> ApplyPseudo(string pseudo, IReadOnlyList<StyleModifier> styleModifiers)
        {
            return GetPseudoFunction(pseudo).Then(pseudoFunction => pseudoFunction([.. styleModifiers]));
        }
    }
}