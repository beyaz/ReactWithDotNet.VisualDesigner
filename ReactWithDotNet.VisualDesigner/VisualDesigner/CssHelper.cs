using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ReactWithDotNet.VisualDesigner;

public static class CssHelper
{
    static readonly Dictionary<string, Func<StyleModifier[], StyleModifier>> MediaQueries = new(StringComparer.OrdinalIgnoreCase)
    {
        { "hover", Hover },
        { "Focus", Focus },
        { "SM", SM },
        { "MD", MD },
        { "LG", LG },
        { "XL", XL },
        { "XXL", XXL }
    };

    public static Result<string> ConvertDesignerStyleItemToTailwindClassName(string designerStyleItemText)
    {
        {
            var pseudo = TryReadPseudo(designerStyleItemText);
            if (pseudo.HasValue is false)
            {
                if (Project.Styles.TryGetValue(designerStyleItemText, out _))
                {
                    return designerStyleItemText;
                }
            }
        }

        DesignerStyleItem designerStyleItem;
        {
            var result = CreateDesignerStyleItemFromText(designerStyleItemText);
            if (result.HasError)
            {
                return result.Error;
            }

            designerStyleItem = result.Value;
        }

        var tailwindClassNames = new List<string>();

        foreach (var (key, value) in designerStyleItem.RawHtmlStyles)
        {
            string tailwindClassName;
            {
                var result = ConvertToTailwindClass(key, value);
                if (result.HasError)
                {
                    return result.Error;
                }

                tailwindClassName = result.Value;
            }

            tailwindClassNames.Add(tailwindClassName);
        }

        // size
        {
            var config = new[]
            {
                new { First = "w-", Second = "h-", Target = "size-" },

                new { First = "pt-", Second = "pb-", Target = "py-" },
                new { First = "pr-", Second = "pl-", Target = "px-" },

                new { First = "mt-", Second = "mb-", Target = "my-" },
                new { First = "mr-", Second = "ml-", Target = "mx-" }
            };

            foreach (var item in config)
            {
                var first = tailwindClassNames.FirstOrDefault(x => x.StartsWith(item.First));
                var second = tailwindClassNames.FirstOrDefault(x => x.StartsWith(item.Second));

                if (first is not null && second is not null)
                {
                    if (first.RemoveFromStart(item.First) == second.RemoveFromStart(item.Second))
                    {
                        tailwindClassNames[tailwindClassNames.IndexOf(first)] = item.Target + first.RemoveFromStart(item.First);

                        tailwindClassNames.Remove(second);
                    }
                }
            }
        }

        if (designerStyleItem.Pseudo is null)
        {
            return string.Join(" ", tailwindClassNames);
        }

        return string.Join(" ", tailwindClassNames.Select(x => designerStyleItem.Pseudo + ":" + x));
    }

    public static NotNullResult<DesignerStyleItem> CreateDesignerStyleItemFromText(string designerStyleItem)
    {
        // try process from plugin
        {
            var result = tryProcessByProjectConfig(designerStyleItem);
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
            var maybe = TryConvertCssUtilityClassToHtmlStyle(designerStyleItem);
            if (maybe.HasValue)
            {
                return new DesignerStyleItem
                {
                    Pseudo        = maybe.Value.Pseudo,
                    RawHtmlStyles = maybe.Value.CssStyles.ToDictionary(x => x.Name, x => x.Value)
                };
            }
        }

        // final calculation
        {
            string name, value, pseudo;
            {
                var attribute = ParseStyleAttibute(designerStyleItem);

                name   = attribute.name;
                value  = attribute.value;
                pseudo = attribute.Pseudo;
            }

            if (value is not null)
            {
                var htmlStyle = ToHtmlStyle(name, value);
                if (htmlStyle.HasError)
                {
                    return htmlStyle.Error;
                }

                return new DesignerStyleItem
                {
                    Pseudo        = pseudo,
                    RawHtmlStyles = htmlStyle.Value
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

        static Result<DesignerStyleItem> tryProcessByProjectConfig(string designerStyleItem)
        {
            string name, value, pseudo;
            {
                var attribute = ParseStyleAttibute(designerStyleItem);

                name   = attribute.name;
                value  = attribute.value;
                pseudo = attribute.Pseudo;

                designerStyleItem = name;

                if (value is not null)
                {
                    designerStyleItem += ":" + value;
                }
            }

            if (Project.Styles.TryGetValue(designerStyleItem, out var cssText))
            {
                return Style.ParseCssAsDictionary(cssText).Then(styleMap => new DesignerStyleItem
                {
                    Pseudo = pseudo,

                    RawHtmlStyles = styleMap
                });
            }

            if (name == "color" && value is not null && Project.Colors.TryGetValue(value, out var realColor))
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

    public static Result<StyleModifier> ToStyleModifier(this DesignerStyleItem designerStyleItem)
    {
        if (designerStyleItem is null)
        {
            throw new ArgumentNullException(nameof(designerStyleItem));
        }

        var style = new Style();

        var exception = style.TryImport(designerStyleItem.RawHtmlStyles);
        if (exception is not null)
        {
            return exception;
        }

        if (designerStyleItem.Pseudo is not null)
        {
            return ApplyPseudo(designerStyleItem.Pseudo, style.ToArray());
        }

        return (StyleModifier)style;
    }

    public static Maybe<(string Pseudo, (string Name, string Value)[] CssStyles)> TryConvertCssUtilityClassToHtmlStyle(string utilityCssClassName)
    {
        string pseudo = null;
        {
            var maybe = TryReadPseudo(utilityCssClassName);
            if (maybe.HasValue)
            {
                pseudo = maybe.Value.Pseudo;

                utilityCssClassName = maybe.Value.NewText;
            }
        }

        switch (utilityCssClassName)
        {
            case "w-full":
            {
                return (pseudo, [("width", "100%")]);
            }

            case "w-fit":
            {
                return (pseudo, [("width", "fit-content")]);
            }
            case "h-full":
            {
                return (pseudo, [("height", "100%")]);
            }
            case "h-fit":
            {
                return (pseudo, [("height", "fit-content")]);
            }
            case "size-fit":
            {
                return (pseudo, [("width", "fit-content"), ("height", "fit-content")]);
            }

            case "flex-row-centered":
            {
                return (pseudo,
                [
                    ("display", "flex"),
                    ("flex-direction", "row"),
                    ("justify-content", "center"),
                    ("align-items", "center")
                ]);
            }
            case "flex-col-centered":
            {
                return (pseudo,
                [
                    ("display", "flex"),
                    ("flex-direction", "column"),
                    ("justify-content", "center"),
                    ("align-items", "center")
                ]);
            }
            case "col":
            {
                return (pseudo,
                [
                    ("display", "flex"),
                    ("flex-direction", "column")
                ]);
            }
            case "row":
            {
                return (pseudo,
                [
                    ("display", "flex")
                ]);
            }
        }

        foreach (var prefix in "m,mt,mb,mr,ml,p,pt,pb,pl,pr".Split(','))
        {
            var numberSuffix = hasMatch(utilityCssClassName, prefix);
            if (numberSuffix.HasValue)
            {
                var styleName = prefix switch
                {
                    "m"  => "margin",
                    "mr" => "margin-right",
                    "ml" => "margin-left",
                    "mt" => "margin-top",
                    "mb" => "margin-bottom",

                    "p"  => "pargin",
                    "pr" => "pargin-right",
                    "pl" => "pargin-left",
                    "pt" => "pargin-top",
                    "pb" => "pargin-bottom",

                    _ => null
                };

                if (styleName is null)
                {
                    return None;
                }

                return (pseudo, [(styleName, numberSuffix.Value * 4 + "px")]);
            }
        }

        // F o n t  W e i g h t
        {
            var fontWeightMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "font-thin", "100" },
                { "font-extralight", "200" },
                { "font-light", "300" },
                { "font-normal", "400" },
                { "font-medium", "500" },
                { "font-semibold", "600" },
                { "font-bold", "700" },
                { "font-extrabold", "800" },
                { "font-black", "900" }
            };

            if (fontWeightMap.TryGetValue(utilityCssClassName, out var weightAsNumber))
            {
                return (pseudo,
                [
                    ("font-weight", weightAsNumber)
                ]);
            }
        }

        // F o n t
        {
            var fontFamilyMap = new Dictionary<string, string>
            {
                { "font-sans", "system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Oxygen', 'Ubuntu', 'Cantarell', 'Fira Sans', 'Droid Sans', 'Helvetica Neue', sans-serif" },
                { "font-serif", "'Georgia', 'Times New Roman', Times, serif" },
                { "font-mono", "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace" }
            };

            if (fontFamilyMap.TryGetValue(utilityCssClassName, out var value))
            {
                return (pseudo,
                [
                    ("font-family", value)
                ]);
            }
        }

        // text - color - weight
        {
            var (success, color, number) = tryParseAs_prefix_color_weight(utilityCssClassName, "text");
            if (success)
            {
                var tailwindColor = tryGetTailwindColor(color, number);
                if (tailwindColor.HasValue)
                {
                    return (pseudo,
                    [
                        ("color", tailwindColor.Value)
                    ]);
                }
            }
        }

        // bg - color - weight
        {
            var (success, color, number) = tryParseAs_prefix_color_weight(utilityCssClassName, "bg");
            if (success)
            {
                var tailwindColor = tryGetTailwindColor(color, number);
                if (tailwindColor.HasValue)
                {
                    return (pseudo,
                    [
                        ("background", tailwindColor.Value)
                    ]);
                }
            }
        }

        // text-decoration-line
        {
            var map = new Dictionary<string, string>
            {
                { "underline", "underline" },
                { "overline", "overline" },
                { "line-through", "line-through" },
                { "no-underline", "none" }
            };

            if (map.TryGetValue(utilityCssClassName, out var value))
            {
                return (pseudo,
                [
                    ("text-decoration-line", value)
                ]);
            }
        }

        // try read from project config
        {
            var (name, value, _) = ParseStyleAttibute(utilityCssClassName);

            if (Project.Styles.TryGetValue(utilityCssClassName, out var cssText))
            {
                var (map, exception) = Style.ParseCssAsDictionary(cssText);
                if (exception is null)
                {
                    return (pseudo, map.Select(x => (x.Key, x.Value)).ToArray());
                }
            }
            else if (name == "color" && value is not null && Project.Colors.TryGetValue(value, out var realColor))
            {
                return (pseudo,
                [
                    ("color", realColor)
                ]);
            }
        }

        return None;

        static double? hasMatch(string text, string prefix)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(prefix))
            {
                return null;
            }

            var pattern = $"^{Regex.Escape(prefix)}-(\\d+(\\.\\d+)?)";

            var match = Regex.Match(text, pattern);

            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }

            return null;
        }

        static (bool success, string color, string number) tryParseAs_prefix_color_weight(string input, string prefix)
        {
            var pattern = $@"{Regex.Escape(prefix)}-(\w+)-(\d+)";
            var regex = new Regex(pattern);
            var match = regex.Match(input);

            if (match.Success)
            {
                var color = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                return (true, color, value);
            }

            return (false, null, null);
        }

        static Maybe<string> tryGetTailwindColor(string colorName, string number)
        {
            var fieldInfo = typeof(Tailwind).GetField(colorName + number, BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public);
            if (fieldInfo != null)
            {
                return (string)fieldInfo.GetValue(null);
            }

            return None;
        }
    }

    static Result<StyleModifier> ApplyPseudo(string pseudo, IReadOnlyList<StyleModifier> styleModifiers)
    {
        return GetPseudoFunction(pseudo).Then(pseudoFunction => pseudoFunction(styleModifiers.ToArray()));
    }

    static Result<string> ConvertToTailwindClass(string name, string value)
    {
        if (value is null)
        {
            return new ArgumentNullException(nameof(value));
        }

        // check is conditional sample: border-width: {props.isSelected} ? 2 : 5
        {
            var conditionalValue = TextParser.TryParseConditionalValue(value);
            if (conditionalValue.success)
            {
                string lefTailwindClass;
                {
                    var result = ConvertToTailwindClass(name, conditionalValue.left);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    lefTailwindClass = result.Value;
                }

                var rightTailwindClass = string.Empty;

                if (conditionalValue.right.HasValue())
                {
                    {
                        var result = ConvertToTailwindClass(name, conditionalValue.right);
                        if (result.HasError)
                        {
                            return result.Error;
                        }

                        rightTailwindClass = result.Value;
                    }
                }

                return "${" + $"{ClearConnectedValue(conditionalValue.condition)} ? '{lefTailwindClass}' : '{rightTailwindClass}'" + '}';
            }
        }

        var isValueDouble = double.TryParse(value, out var valueAsDouble);

        name = name switch
        {
            "padding"        => "p",
            "padding-right"  => "pr",
            "padding-left"   => "pl",
            "padding-top"    => "pt",
            "padding-bottom" => "pb",

            "margin"        => "m",
            "margin-right"  => "mr",
            "margin-left"   => "ml",
            "margin-top"    => "mt",
            "margin-bottom" => "mb",

            _ => name
        };

        switch (name)
        {
            case "transform":
            {
                if (value.StartsWith("rotate("))
                {
                    var parts = value.Split("()".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var sign = parts[1][0] == '-' ? "-" : "";
                        if (parts[1].EndsWith("deg"))
                        {
                            return $"{sign}rotate-{value.RemoveFromEnd("deg")}";
                        }
                    }
                }

                break;
            }
            case "outline":
            {
                return $"{name}-{value}";
            }

            case "text-decoration":
            {
                return $"{value}";
            }

            case "text-align":
            {
                return $"text-{value}";
            }

            case "width":
            {
                if (value == "fit-content")
                {
                    return "w-fit";
                }

                if (value == "100%")
                {
                    return "w-full";
                }

                return $"w-[{value}]";
            }

            case "height":
            {
                if (value == "fit-content")
                {
                    return "h-fit";
                }

                if (value == "100%")
                {
                    return "h-full";
                }

                return $"h-[{value}]";
            }

            case "max-width":
                return $"max-w-[{value}]";

            case "max-height":
                return $"max-h-[{value}]";

            case "min-width":
                return $"min-w-[{value}]";

            case "min-height":
                return $"min-h-[{value}]";

            case "z-index":
                return $"z-[{value}]";

            case "overflow-y":
            case "overflow-x":
                return $"{name}-{value}";

            case "border-top-left-radius":
                return $"rounded-tl-[{value}]";

            case "border-top-right-radius":
                return $"rounded-tr-[{value}]";

            case "border-bottom-left-radius":
                return $"rounded-bl-[{value}]";

            case "border-bottom-right-radius":
                return $"rounded-br-[{value}]";

            case "flex-grow":
                return $"flex-grow-[{value}]";

            case "border-bottom-width":
                return $"border-b-[{value}]";

            case "border-top-width":
                return $"border-t-[{value}]";

            case "border-left-width":
                return $"border-l-[{value}]";

            case "border-right-width":
                return $"border-r-[{value}]";

            case "border-top":
            case "border-right":
            case "border-left":
            case "border-bottom":
            {
                var direction = name.Split('-', StringSplitOptions.RemoveEmptyEntries).Last();

                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3)
                {
                    if (Project.Colors.TryGetValue(parts[2], out var htmlColor))
                    {
                        parts[2] = htmlColor;
                    }

                    var directionShortName = direction switch
                    {
                        "top"    => "t",
                        "bottom" => "b",
                        "right"  => "r",
                        "left"   => "l",
                        _        => null
                    };

                    if (directionShortName is null)
                    {
                        return new ArgumentOutOfRangeException(direction);
                    }

                    return $"border-{directionShortName}-[{parts[0]}]" +
                           $" [border-{direction}-style:{parts[1]}]" +
                           $" [border-{direction}-color:{parts[2]}]";
                }

                return new ArgumentOutOfRangeException(direction);
            }

            case "display":
                return $"{value}";

            case "color":
            {
                if (Project.Colors.TryGetValue(value, out var htmlColor))
                {
                    value = htmlColor;
                }

                return $"text-[{value}]";
            }

            case "border-color":
            {
                if (Project.Colors.TryGetValue(value, out var htmlColor))
                {
                    value = htmlColor;
                }

                return $"border-[{value}]";
            }

            case "gap":
                return $"gap-[{value}]";

            case "size":
                return $"size-[{value}]";

            case "bottom":
            case "top":
            case "left":
            case "right":
                return $"{name}-[{value}]";

            case "flex-direction":
            {
                if (value == "column")
                {
                    return "flex-col";
                }

                if (value == "row")
                {
                    return "flex";
                }

                break;
            }

            case "align-items":
                return $"items-{value.RemoveFromStart("align-")}";

            case "justify-content":
                return $"justify-{value.Split('-').Last()}";

            case "border-radius":
                return $"rounded-[{value}]";

            case "font-size":
                return $"[font-size:{value}]";

            case "border-width":
            {
                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return $"border-[{value}]";
            }

            case "m":
            case "mx":
            case "my":
            case "ml":
            case "mr":
            case "mb":
            case "mt":

            case "p":
            case "px":
            case "py":
            case "pl":
            case "pr":
            case "pb":
            case "pt":
            {
                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return $"{name}-[{value}]";
            }

            case "border":
            {
                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3)
                {
                    if (Project.Colors.TryGetValue(parts[2], out var htmlColor))
                    {
                        parts[2] = htmlColor;
                    }

                    if (parts[0] == "1px" && parts[1] == "solid")
                    {
                        return "border " +
                               $"border-[{parts[2]}]";
                    }

                    return $"border-[{parts[0]}] " +
                           $"border-[{parts[1]}] " +
                           $"border-[{parts[2]}]";
                }

                break;
            }
            case "background":
            case "bg":
            {
                if (Project.Colors.TryGetValue(value, out var htmlColor))
                {
                    value = htmlColor;
                }

                return $"bg-[{value}]";
            }
            case "position":
                return $"{value}";

            case "border-style":
            {
                return $"border-{value}";
            }

            case "cursor":
            {
                return $"cursor-{value}";
            }

            case "inset":
            {
                return $"inset-{value}";
            }

            case "font-family":
            {
                return $"font-[{value}]";
            }
            case "font-style":
            {
                return $"[font-style:{value}]";
            }
            case "font-weight":
            {
                return $"[font-weight:{value}]";
            }
            case "line-height":
            {
                return $"[line-height:{value}]";
            }
        }

        return new InvalidOperationException($"Css not handled. {name}: {value}");
    }

    static Result<Func<StyleModifier[], StyleModifier>> GetPseudoFunction(string pseudoName)
    {
        if (MediaQueries.TryGetValue(pseudoName, out var func))
        {
            return func;
        }

        return new ArgumentOutOfRangeException($"{pseudoName} not recognized");
    }

    static HtmlStyle ToHtmlStyle(string name, string value)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var isValueDouble = double.TryParse(value, out var valueAsDouble);

        name = name switch
        {
            "p"  => "padding",
            "pl" => "padding-left",
            "pr" => "padding-right",
            "pt" => "padding-top",
            "pb" => "padding-bottom",

            "m"  => "margin",
            "ml" => "margin-left",
            "mr" => "margin-right",
            "mt" => "margin-top",
            "mb" => "margin-bottom",

            "w" => "width",
            "h" => "height",

            "bg" => "background",

            _ => name
        };

        switch (name)
        {
            // AS PIXEL
            case "width":
            case "height":
            case "max-width":
            case "max-height":
            case "min-width":
            case "min-height":
            case "inset":
            case "border-width":
            case "border-bottom-width":
            case "border-top-right-radius":
            case "border-top-left-radius":
            case "border-bottom-left-radius":
            case "border-bottom-right-radius":
            case "font-size":
            case "left":
            case "right":
            case "bottom":
            case "top":
            case "padding":
            case "padding-left":
            case "padding-right":
            case "padding-top":
            case "padding-bottom":
            case "margin":
            case "margin-left":
            case "margin-right":
            case "margin-top":
            case "margin-bottom":
            case "gap":
            case "border-radius":
            {
                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return (name, value);
            }

            // S A M E
            case "align-items":
            case "justify-items":
            case "justify-content":
            case "display":
            case "font-weight":
            case "flex-direction":
            case "z-index":
            case "position":
            case "overflow-y":
            case "overflow-x":
            case "fill":
            case "stroke":
            case "border-color":
            case "font-family":
            case "cursor":
            case "border-style":
            case "text-align":
            case "flex-grow":
            case "outline":
            case "text-decoration":
            {
                return (name, value);
            }

            case "border-top":
            case "border-bottom":
            case "border-left":
            case "border-right":
            case "border":
            {
                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3)
                {
                    for (var i = 0; i < parts.Length; i++)
                    {
                        if (Project.Colors.TryGetValue(parts[i], out var color))
                        {
                            parts[i] = color;
                        }
                    }

                    value = string.Join(" ", parts);
                }

                return (name, value);
            }

            // m u l t i p l e
            case "px":
            {
                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return new[] { ("padding-left", value), ("padding-right", value) };
            }
            case "py":
            {
                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return new[] { ("padding-top", value), ("padding-bottom", value) };
            }
            case "size":
            {
                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return new[] { ("width", value), ("height", value) };
            }

            // c o l o r s
            case "background":
            case "color":
            {
                if (Project.Colors.TryGetValue(value, out var realColor))
                {
                    value = realColor;
                }

                return (name, value);
            }

            // SPECIAL
            case "transform":
            {
                if (isValueDouble)
                {
                    value = valueAsDouble + "deg";
                }

                return (name, value);
            }
        }

        return new Exception($"{name}: {value} is not recognized");
    }

    static Maybe<(string Pseudo, string NewText)> TryReadPseudo(string text)
    {
        foreach (var pseudo in MediaQueries.Keys)
        {
            var prefix = pseudo + ":";
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var newText = text.RemoveFromStart(prefix, StringComparison.OrdinalIgnoreCase);

                return (prefix.ToLower(), newText);
            }
        }

        return None;
    }

    static class TextParser
    {
        public static (bool success, string condition, string left, string right) TryParseConditionalValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return (false, null, null, null);
            }

            // condition ? left : right  (right opsiyonel)
            var pattern = @"^\s*(?<condition>[^?]+?)\s*\?\s*(?<left>[^:]+?)\s*(?::\s*(?<right>.+))?$";
            var match = Regex.Match(value, pattern);

            if (match.Success)
            {
                var condition = match.Groups["condition"].Value.Trim();
                var left = match.Groups["left"].Value.Trim();
                var right = match.Groups["right"].Success ? match.Groups["right"].Value.Trim() : null;
                return (true, condition, left, right);
            }

            return (false, null, null, null);
        }
    }
}

public sealed record DesignerStyleItem
{
    public string Pseudo { get; init; }

    public IReadOnlyDictionary<string, string> RawHtmlStyles { get; init; }

    public static implicit operator DesignerStyleItem((string Pseudo, (string Name, string Value)[] RawHtmlStyles) tuple)
    {
        return new()
        {
            Pseudo        = tuple.Pseudo,
            RawHtmlStyles = tuple.RawHtmlStyles.ToDictionary(x => x.Name, x => x.Value)
        };
    }
}

public sealed class HtmlStyle : Result<Dictionary<string, string>>
{
    public static implicit operator HtmlStyle((string Name, string Value) item)
    {
        return new()
        {
            Success = true,
            Value = new()
            {
                [item.Name] = item.Value
            }
        };
    }

    public static implicit operator HtmlStyle((string Name, string Value)[] items)
    {
        return new()
        {
            Success = true,
            Value   = items.ToDictionary(x => x.Name, x => x.Value)
        };
    }

    public static implicit operator HtmlStyle(Exception exception)
    {
        return new()
        {
            Success = false,
            Error   = exception
        };
    }
}