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

    public static Result<StyleModifier> ApplyPseudo(string pseudo, IReadOnlyList<StyleModifier> styleModifiers)
    {
        return GetPseudoFunction(pseudo).Then(pseudoFunction => pseudoFunction(styleModifiers.ToArray()));
    }

    public static Result<string> ConvertDesignerStyleItemToTailwindClassName(string designerStyleItem)
    {
        var parseResult = TryParsePropertyValue(designerStyleItem);
        if (parseResult.success)
        {
            return ConvertToTailwindClass(parseResult.name, parseResult.value);
        }

        return designerStyleItem;
    }

    public static Maybe<(string Pseudo, (string Name, string Value)[] CssStyles)> ConvertDesignerStyleItemToHtmlStyle(string designerStyleItem)
    {
         // try process from plugin
        {
            var style = TryProcessStyleAttributeByProjectConfig(styleAttribute);
            if (!style.Success)
            {
                return style.Error;
            }

            if (style.Value is not null)
            {
                return new[] { style.Value };
            }
        }

        {
            var maybe = TryConvertCssUtilityClassToHtmlStyle(styleAttribute);
            if (maybe.HasValue)
            {
                var pseudo = maybe.Value.Pseudo;

                var cssStyles = maybe.Value.CssStyles;

                Func<StyleModifier[], StyleModifier> pseudoFunction = null;

                if (pseudo is not null)
                {
                    var result = GetPseudoFunction(pseudo);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    pseudoFunction = result.Value;
                }

                return cssStyles.Select(x => CssHelper.ConvertToStyleModifier(x.Name, x.Value)).Then(styleModifiers =>
                {
                    if (pseudoFunction is not null)
                    {
                        return [pseudoFunction(styleModifiers.ToArray())];
                    }

                    return styleModifiers;
                });
            }
        }

        // final calculation
        {
            string name, value, pseudo;
            {
                var attribute = ParseStyleAttibute(styleAttribute);

                name   = attribute.name;
                value  = attribute.value;
                pseudo = attribute.Pseudo;
            }

            var styleModifiers = CssHelper.ConvertToStyleModifier(name, value);

            if (pseudo is not null)
            {
                return ApplyPseudo(pseudo, [styleModifiers.Value]).ToReadOnlyList();
            }

            return styleModifiers.ToReadOnlyList();
        }

        static Result<StyleModifier> TryProcessStyleAttributeByProjectConfig(string styleAttribute)
        {
            StyleModifier modifier = null;

            string name, value, pseudo;
            {
                var attribute = ParseStyleAttibute(styleAttribute);

                name   = attribute.name;
                value  = attribute.value;
                pseudo = attribute.Pseudo;

                styleAttribute = name;

                if (value is not null)
                {
                    styleAttribute += ":" + value;
                }
            }

            if (Project.Styles.TryGetValue(styleAttribute, out var cssText))
            {
                modifier = Style.ParseCss(cssText);
            }
            else if (name == "color" && value is not null && Project.Colors.TryGetValue(value, out var realColor))
            {
                modifier = Color(realColor);
            }

            if (modifier is not null)
            {
                if (pseudo is not null)
                {
                    return ApplyPseudo(pseudo, [modifier]);
                }

                return modifier;
            }

            return None;
        }
    }
    
    public static Result<StyleModifier> ConvertToStyleModifier(string name, string value)
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

        switch (name)
        {
            case "transform":
            {
                if (isValueDouble)
                {
                    return Transform(valueAsDouble + "deg");
                }

                return Transform(value);
            }
            case "min-width":
            {
                if (isValueDouble)
                {
                    return MinWidth(valueAsDouble);
                }

                return MinWidth(value);
            }

            case "top":
            {
                if (isValueDouble)
                {
                    return Top(valueAsDouble);
                }

                return Top(value);
            }
            case "bottom":
            {
                if (isValueDouble)
                {
                    return Bottom(valueAsDouble);
                }

                return Bottom(value);
            }
            case "left":
            {
                if (isValueDouble)
                {
                    return Left(valueAsDouble);
                }

                return Left(value);
            }
            case "right":
            {
                if (isValueDouble)
                {
                    return Right(valueAsDouble);
                }

                return Right(value);
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

                switch (name)
                {
                    case "border-top":
                        return BorderTop(value);
                    case "border-bottom":
                        return BorderBottom(value);
                    case "border-left":
                        return BorderLeft(value);
                    case "border-right":
                        return BorderRight(value);
                    default:
                        return Border(value);
                }
            }

            case "justify-items":
            {
                return JustifyItems(value);
            }
            case "justify-content":
            {
                return JustifyContent(value);
            }

            case "align-items":
            {
                return AlignItems(value);
            }

            case "display":
            {
                return Display(value);
            }

            case "background":
            case "bg":
            {
                if (Project.Colors.TryGetValue(value, out var realColor))
                {
                    value = realColor;
                }

                return Background(value);
            }

            case "font-size":
            {
                if (isValueDouble)
                {
                    return FontSize(valueAsDouble);
                }

                return FontSize(value);
            }

            case "font-weight":
            {
                return FontWeight(value);
            }

            case "text-align":
            {
                return TextAlign(value);
            }

            case "w":
            case "width":
            {
                if (isValueDouble)
                {
                    return Width(valueAsDouble);
                }

                return Width(value);
            }

            case "outline":
            {
                return Outline(value);
            }

            case "text-decoration":
            {
                return TextDecoration(value);
            }

            case "h":
            case "height":
            {
                if (isValueDouble)
                {
                    return Height(valueAsDouble);
                }

                return Height(value);
            }

            case "border-radius":
            {
                if (isValueDouble)
                {
                    return BorderRadius(valueAsDouble);
                }

                return BorderRadius(value);
            }

            case "gap":
            {
                if (isValueDouble)
                {
                    return Gap(valueAsDouble);
                }

                return Gap(value);
            }
            case "flex-grow":
            {
                if (isValueDouble)
                {
                    return FlexGrow(valueAsDouble);
                }

                return FlexGrow(value);
            }

            case "p":
            case "padding":
            {
                if (isValueDouble)
                {
                    return Padding(valueAsDouble);
                }

                return Padding(value);
            }

            case "size":
            {
                if (isValueDouble)
                {
                    return Size(valueAsDouble);
                }

                return Size(value);
            }

            case "color":
            {
                if (Project.Colors.TryGetValue(value, out var realColor))
                {
                    value = realColor;
                }

                return Color(value);
            }

            case "px":
            {
                if (isValueDouble)
                {
                    return PaddingLeftRight(valueAsDouble);
                }

                return PaddingLeftRight(value);
            }
            case "py":
            {
                if (isValueDouble)
                {
                    return PaddingTopBottom(valueAsDouble);
                }

                return PaddingTopBottom(value);
            }

            case "pl":
            case "padding-left":
            {
                if (isValueDouble)
                {
                    return PaddingLeft(valueAsDouble);
                }

                return PaddingLeft(value);
            }

            case "pr":
            case "padding-right":
            {
                if (isValueDouble)
                {
                    return PaddingRight(valueAsDouble);
                }

                return PaddingRight(value);
            }

            case "pt":
            case "padding-top":
            {
                if (isValueDouble)
                {
                    return PaddingTop(valueAsDouble);
                }

                return PaddingTop(value);
            }

            case "pb":
            case "padding-bottom":
            {
                if (isValueDouble)
                {
                    return PaddingBottom(valueAsDouble);
                }

                return PaddingBottom(value);
            }

            case "ml":
            case "margin-left":
            {
                if (isValueDouble)
                {
                    return MarginLeft(valueAsDouble);
                }

                return MarginLeft(value);
            }

            case "mr":
            case "margin-right":
            {
                if (isValueDouble)
                {
                    return MarginRight(valueAsDouble);
                }

                return MarginRight(value);
            }

            case "mt":
            case "margin-top":
            {
                if (isValueDouble)
                {
                    return MarginTop(valueAsDouble);
                }

                return MarginTop(value);
            }

            case "mb":
            case "margin-bottom":
            {
                if (isValueDouble)
                {
                    return MarginBottom(valueAsDouble);
                }

                return MarginBottom(value);
            }

            case "flex-direction":
            {
                return FlexDirection(value);
            }
            case "z-index":
            {
                return ZIndex(value);
            }
            case "position":
            {
                return Position(value);
            }
            case "max-width":
            {
                if (isValueDouble)
                {
                    return MaxWidth(valueAsDouble);
                }

                return MaxWidth(value);
            }
            case "max-height":
            {
                if (isValueDouble)
                {
                    return MaxHeight(valueAsDouble);
                }

                return MaxHeight(value);
            }
            case "border-top-left-radius":
            {
                return BorderTopLeftRadius(valueAsDouble);
            }
            case "border-top-right-radius":
            {
                return BorderTopRightRadius(valueAsDouble);
            }
            case "border-bottom-left-radius":
            {
                return BorderBottomLeftRadius(valueAsDouble);
            }
            case "border-bottom-right-radius":
            {
                return BorderBottomRightRadius(valueAsDouble);
            }

            case "overflow-y":
            {
                return OverflowY(value);
            }
            case "overflow-x":
            {
                return OverflowX(value);
            }
            case "border-bottom-width":
            {
                if (isValueDouble)
                {
                    return BorderBottomWidth(valueAsDouble + "px");
                }

                return BorderBottomWidth(value);
            }
            case "fill":
            {
                return Fill(value);
            }
            case "stroke":
            {
                return Stroke(value);
            }

            case "font-family":
            {
                return FontFamily(value);
            }

            case "border-color":
            {
                return BorderColor(value);
            }
            case "border-style":
            {
                return BorderStyle(value);
            }
            case "border-width":
            {
                if (isValueDouble)
                {
                    return BorderWidth(valueAsDouble);
                }

                return BorderWidth(value);
            }
            case "cursor":
            {
                return Cursor(value);
            }
            case "inset":
            {
                if (isValueDouble)
                {
                    return Inset(valueAsDouble + "px");
                }

                return Inset(value);
            }
        }

        return new Exception($"{name}: {value} is not recognized");
    }

    public static Result<Func<StyleModifier[], StyleModifier>> GetPseudoFunction(string pseudoName)
    {
        if (MediaQueries.TryGetValue(pseudoName, out var func))
        {
            return func;
        }

        return new ArgumentOutOfRangeException($"{pseudoName} not recognized");
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
                    ("display", "flex"),
                    ("flex-direction", "row")
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

            case "W":
            case "w":
            case "width":
            {
                if (value == "fit-content")
                {
                    return "w-fit";
                }

                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return $"w-[{value}]";
            }

            case "text-align":
            {
                return $"text-{value}";
            }

            case "h":
            case "height":
            {
                if (value == "fit-content")
                {
                    return "h-fit";
                }

                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return $"h-[{value}]";
            }

            case "max-width":
                return $"max-w-[{value}px]";

            case "max-height":
                return $"max-h-[{value}px]";

            case "min-width":
                return $"min-w-[{value}px]";

            case "min-height":
                return $"min-h-[{value}px]";

            case "z-index":
                return $"z-[{value}]";

            case "overflow-y":
            case "overflow-x":
                return $"{name}-{value}";

            case "border-top-left-radius":
                return $"rounded-tl-[{value}px]";

            case "border-top-right-radius":
                return $"rounded-tr-[{value}px]";

            case "border-bottom-left-radius":
                return $"rounded-bl-[{value}px]";

            case "border-bottom-right-radius":
                return $"rounded-br-[{value}px]";

            case "flex-grow":
                return $"flex-grow-[{value}]";

            case "border-bottom-width":
                return $"border-b-[{value}px]";

            case "border-top-width":
                return $"border-t-[{value}px]";

            case "border-left-width":
                return $"border-l-[{value}px]";

            case "border-right-width":
                return $"border-r-[{value}px]";

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
                return $"gap-[{value}px]";

            case "size":
                return $"size-[{value}px]";

            case "bottom":
            case "top":
            case "left":
            case "right":
                return $"{name}-[{value}px]";

            case "flex-direction" when value == "column":
                return "flex-col";

            case "align-items":
                return $"items-{value.RemoveFromStart("align-")}";

            case "justify-content":
                return $"justify-{value.Split('-').Last()}";

            case "border-radius":
                return $"rounded-[{value}px]";

            case "font-size":
                return $"[font-size:{value}px]";

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
        }

        return new InvalidOperationException($"Css not handled. {name}: {value}");
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