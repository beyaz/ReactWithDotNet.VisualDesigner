using System.Reflection;

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

                return new DesignerStyleItem
                {
                    Pseudo = pseudo,
                    RawHtmlStyles = new Dictionary<string, string>
                    {
                        [htmlStyle.Value.name] = htmlStyle.Value.value
                    }
                };
            }

            return new DesignerStyleItem
            {
                Pseudo = pseudo,
                RawHtmlStyles = new Dictionary<string, string>
                {
                    { name, null }
                }
            };
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
                return Style.ParseCssAsDictionary(cssText).Then(styleMap => new DesignerStyleItem
                {
                    Pseudo = pseudo,

                    RawHtmlStyles = styleMap
                });
            }

            if (name == "color" && value is not null && project.Colors.TryGetValue(value, out var realColor))
            {
                return new DesignerStyleItem
                {
                    Pseudo = pseudo,

                    RawHtmlStyles = new Dictionary<string, string>
                    {
                        { "color", realColor }
                    }
                };
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

        foreach (var (name, value) in arrangeRawHtmlStyles(designerStyleItem.RawHtmlStyles))
        {
            var exception = style.TrySet(name, tryFixValueForBorderColor(name, value));
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

        static string tryFixValueForBorderColor(string styleAttributeName, string styleAttributeValue)
        {
            if (styleAttributeName == "border")
            {
                var valueParts = styleAttributeValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (valueParts.Length == 3)
                {
                    if (valueParts[0].EndsWith("px"))
                    {
                        var fieldInfo = typeof(Tailwind).GetField(valueParts[2], BindingFlags.Static | BindingFlags.Public);
                        if (fieldInfo is not null)
                        {
                            return string.Join(' ', valueParts[0], valueParts[1], fieldInfo.GetValue(null));
                        }
                    }
                }
            }

            return styleAttributeValue;
        }

        static IReadOnlyDictionary<string, string> arrangeRawHtmlStyles(IReadOnlyDictionary<string, string> dictionary)
        {
            var map = new Dictionary<string, string>();

            foreach (var (key, value) in dictionary)
            {
                var item = arrangeHtmlStyleValue(key, value);

                map.Add(item.key, item.value);
            }

            return map;

            static (string key, string value) arrangeHtmlStyleValue(string key, string value)
            {
                var (success, _, left, right) = TryParseConditionalValue(value);
                if (success)
                {
                    if (right is not null)
                    {
                        return (key, right);
                    }

                    if (left is not null)
                    {
                        return (key, left);
                    }
                }

                return (key, value);
            }
        }

        static Result<StyleModifier> ApplyPseudo(string pseudo, IReadOnlyList<StyleModifier> styleModifiers)
        {
            return GetPseudoFunction(pseudo).Then(pseudoFunction => pseudoFunction([.. styleModifiers]));
        }
    }
}

public sealed record DesignerStyleItem
{
    public string OriginalText { get; init; }
    
    public string Pseudo { get; init; }

    public IReadOnlyDictionary<string, string> RawHtmlStyles { get; init; }

    public static implicit operator DesignerStyleItem((string Pseudo, (string Name, string Value)[] RawHtmlStyles) tuple)
    {
        foreach (var (name, value) in tuple.RawHtmlStyles)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Style name cannot be null or whitespace.", nameof(tuple));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Style value cannot be whitespace.", nameof(tuple));
            }
        }

        return new()
        {
            Pseudo        = tuple.Pseudo,
            RawHtmlStyles = tuple.RawHtmlStyles.ToDictionary(x => x.Name, x => x.Value)
        };
    }
}

public sealed record FinalStyleAttribute
{
    public required string Name { get; init; }
    
    public required string Value { get; init; }

    public override string ToString()
    {
        return $"{Name}: {Value};";
    }
}