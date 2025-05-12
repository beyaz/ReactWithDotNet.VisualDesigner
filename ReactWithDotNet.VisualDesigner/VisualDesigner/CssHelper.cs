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

    public static Result<string> ConvertDesignerStyleItemToTailwindClassName(int projectId, string designerStyleItemText)
    {
        string pseudo = null;

        TryReadPseudo(designerStyleItemText).HasValue(x =>
        {
            pseudo = x.Pseudo;

            designerStyleItemText = x.NewText;
        });

        var project = GetProjectConfig(projectId);
        
        if (pseudo.HasNoValue() && project.Styles.TryGetValue(designerStyleItemText, out _))
        {
            return designerStyleItemText;
        }

        var tailwindClassNames = new List<string>();
        {
            DesignerStyleItem designerStyleItem;
            {
                var result = CreateDesignerStyleItemFromText(projectId, designerStyleItemText);
                if (result.HasError)
                {
                    return result.Error;
                }

                designerStyleItem = result.Value;
            }

            foreach (var (key, value) in designerStyleItem.RawHtmlStyles)
            {
                string tailwindClassName;
                {
                    var result = ConvertToTailwindClass(project, key, value);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    tailwindClassName = result.Value;
                }

                tailwindClassNames.Add(tailwindClassName);
            }
        }

        // try to combine shorthand tailwind class names
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

        if (pseudo is null)
        {
            return string.Join(" ", tailwindClassNames);
        }

        return string.Join(" ", tailwindClassNames.Select(x => pseudo + ":" + x));
    }

    public static NotNullResult<DesignerStyleItem> CreateDesignerStyleItemFromText(int projectId, string designerStyleItem)
    {
        // try process from plugin
        {
            var result = tryProcessByProjectConfig(projectId, designerStyleItem);
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
            var maybe = TryConvertCssUtilityClassToHtmlStyle(projectId, designerStyleItem);
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

                name   = attribute.Name;
                value  = attribute.Value;
                pseudo = attribute.Pseudo;
            }

            if (value is not null)
            {
                var htmlStyle = ToHtmlStyle(projectId,name, value);
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

        static Result<DesignerStyleItem> tryProcessByProjectConfig(int projectId,string designerStyleItem)
        {
            string name, value, pseudo;
            {
                var attribute = ParseStyleAttibute(designerStyleItem);

                name   = attribute.Name;
                value  = attribute.Value;
                pseudo = attribute.Pseudo;

                designerStyleItem = name;

                if (value is not null)
                {
                    designerStyleItem += ":" + value;
                }
            }

            var project = GetProjectConfig(projectId);
            
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

    public static Maybe<(string Pseudo, (string Name, string Value)[] CssStyles)> TryConvertCssUtilityClassToHtmlStyle(int projectId,string utilityCssClassName)
    {
        string pseudo = null;
        {
            TryReadPseudo(utilityCssClassName).HasValue(x =>
            {
                pseudo = x.Pseudo;
                
                utilityCssClassName = x.NewText;
            });
        }

        // try resolve from project config
        var project = GetProjectConfig(projectId);
        {
            if (project.Styles.TryGetValue(utilityCssClassName, out var cssText))
            {
                var (map, exception) = Style.ParseCssAsDictionary(cssText);
                if (exception is null)
                {
                    return (pseudo, map.Select(x => (x.Key, x.Value)).ToArray());
                }
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

                    "p"  => "padding",
                    "pr" => "padding-right",
                    "pl" => "padding-left",
                    "pt" => "padding-top",
                    "pb" => "padding-bottom",

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
            if (name == "color" && value is not null && project.Colors.TryGetValue(value, out var realColor))
            {
                return (pseudo,
                [
                    ("color", realColor)
                ]);
            }
        }
        
        // try to handle by spacing scale or arbitrary value
        {
            var maybe = TailwindSpacingScale.Try_Convert_From_TailwindClass_to_HtmlStyle(utilityCssClassName);
            if (maybe.HasValue)
            {
                return (pseudo,
                [
                    maybe.Value
                ]);
            }
        }
        
        // tailwindClass-[?px]
        {
            var map = new Dictionary<string, string>
            {
                { "rounded", "border-radius" },
                { "h", "height" },
                { "w", "width" }
            };
            
            foreach (var (tailwindPrefix, styleName) in map)
            {
                var arbitrary = tryGetArbitraryValue(utilityCssClassName, tailwindPrefix);
                if (arbitrary.HasValue && arbitrary.Value.EndsWith("px"))
                {
                    return (pseudo,
                    [
                        (styleName, arbitrary.Value)
                    ]);
                }
            }
        }
        
        // tailwindClass-[color]
        {
            var map = new Dictionary<string, string>
            {
                { "border", "border" },
                { "border-t", "border-top" },
                { "border-b", "border-bottom" },
                { "border-l", "border-left" },
                { "border-r", "border-right" },
                { "bg", "background" },
                { "text", "color" }
            };

            foreach (var (tailwindPrefix,styleName) in map)
            {
                var arbitrary = tryGetArbitraryValue(utilityCssClassName, tailwindPrefix);
                if (arbitrary.HasValue)
                {
                    var color = arbitrary.Value;
                    if (project.Colors.TryGetValue(color, out var realColor))
                    {
                        color = realColor;
                    }
                
                    return (pseudo,
                    [
                        (styleName, color)
                    ]);
                }
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
        
        static Maybe<string> tryGetArbitraryValue(string input, string prefix)
        {
            // Örn: prefix = "rounded" --> pattern = ^rounded-\[(.+?)\]$
            string pattern = $@"^{Regex.Escape(prefix)}-\[(.+?)\]$";
            var match = Regex.Match(input, pattern);

            return match.Success ? match.Groups[1].Value : None;
        }
    }
    static readonly IReadOnlyDictionary<string, double> TailwindSpacingScaleMap = new Dictionary<string, double>
    {
        { "0px", 0 },
        { "2px", 0.5 },
        { "4px", 1 },
        { "6px", 1.5 },
        { "8px", 2 },
        { "10px", 2.5 },
        { "12px", 3 },
        { "14px", 3.5 },
        { "16px", 4 },
        { "20px", 5 },
        { "24px", 6 },
        { "28px", 7 },
        { "32px", 8 },
        { "36px", 9 },
        { "40px", 10 },
        { "44px", 11 },
        { "48px", 12 },
        { "56px", 14 },
        { "64px", 16 },
        { "80px", 20 },
        { "96px", 24 },
        { "112px", 28 },
        { "128px", 32 },
        { "144px", 36 },
        { "160px", 40 },
        { "176px", 44 },
        { "192px", 48 },
        { "208px", 52 },
        { "224px", 56 },
        { "240px", 60 },
        { "256px", 64 },
        { "288px", 72 },
        { "320px", 80 },
        { "384px", 96 }
    };
    static Result<StyleModifier> ApplyPseudo(string pseudo, IReadOnlyList<StyleModifier> styleModifiers)
    {
        return GetPseudoFunction(pseudo).Then(pseudoFunction => pseudoFunction(styleModifiers.ToArray()));
    }

  

    static class TailwindSpacingScale
    {
        static readonly (string htmlStyleName, string tailwindPrefix)[] Html_to_TailwindName_Map =
        [
            (htmlStyleName: "min-width", tailwindPrefix: "min-w-"),
            (htmlStyleName: "min-height", tailwindPrefix: "min-h-"),
            (htmlStyleName: "max-width", tailwindPrefix: "max-w-"),
            (htmlStyleName: "max-height", tailwindPrefix: "max-h-"),

            // ↔ Width & Height
            (htmlStyleName: "width", tailwindPrefix: "w-"),
            (htmlStyleName: "height", tailwindPrefix: "h-"),

            // Padding
            (htmlStyleName: "padding", tailwindPrefix: "p-"),
            (htmlStyleName: "padding-top", tailwindPrefix: "pt-"),
            (htmlStyleName: "padding-right", tailwindPrefix: "pr-"),
            (htmlStyleName: "padding-bottom", tailwindPrefix: "pb-"),
            (htmlStyleName: "padding-left", tailwindPrefix: "pl-"),
            (htmlStyleName: "padding-inline", tailwindPrefix: "px-"),
            (htmlStyleName: "padding-block", tailwindPrefix: "py-"),

            // Margin
            (htmlStyleName: "margin", tailwindPrefix: "m-"),
            (htmlStyleName: "margin-top", tailwindPrefix: "mt-"),
            (htmlStyleName: "margin-right", tailwindPrefix: "mr-"),
            (htmlStyleName: "margin-bottom", tailwindPrefix: "mb-"),
            (htmlStyleName: "margin-left", tailwindPrefix: "ml-"),
            (htmlStyleName: "margin-inline", tailwindPrefix: "mx-"),
            (htmlStyleName: "margin-block", tailwindPrefix: "my-"),

            // Gap (for flex/grid gaps)
            (htmlStyleName: "gap", tailwindPrefix: "gap-"),
            (htmlStyleName: "row-gap", tailwindPrefix: "gap-y-"),
            (htmlStyleName: "column-gap", tailwindPrefix: "gap-x-"),

            // Inset (top/right/bottom/left for positioning)
            (htmlStyleName: "top", tailwindPrefix: "top-"),
            (htmlStyleName: "right", tailwindPrefix: "right-"),
            (htmlStyleName: "bottom", tailwindPrefix: "bottom-"),
            (htmlStyleName: "left", tailwindPrefix: "left-"),

            // Space-between (child spacing in flex)
            (htmlStyleName: "space-x", tailwindPrefix: "space-x-"),
            (htmlStyleName: "space-y", tailwindPrefix: "space-y-"),

            // Translate (transform)
            (htmlStyleName: "translate-x", tailwindPrefix: "translate-x-"),
            (htmlStyleName: "translate-y", tailwindPrefix: "translate-y-")
        ];

        public static Maybe<string> Try_Convert_From_HtmlStyle_to_TailwindClass(string name, string value)
        {
            foreach (var item in Html_to_TailwindName_Map)
            {
                if (item.htmlStyleName == name)
                {
                    var maybe = try_Convert_PixelValue_To_SpacingScale_Or_ArbitraryValue(value);
                    if (maybe.HasValue)
                    {
                        return item.tailwindPrefix + maybe.Value;
                    }
                }
            }

            return None;
            
            static Maybe<string> try_Convert_PixelValue_To_SpacingScale_Or_ArbitraryValue(string value)
            {
                if (!value.EndsWith("px"))
                {
                    return None;
                }
                    
                if (TailwindSpacingScaleMap.TryGetValue(value, out var spaceValue))
                {
                    return spaceValue.ToString(CultureInfo_en_US);
                }
                    
                return "[" + value + "]";
            }
        }
        
        public static Maybe<(string htmlStyleName, string htmlStyleValue)> Try_Convert_From_TailwindClass_to_HtmlStyle(string tailwindClass)
        {
            foreach (var item in Html_to_TailwindName_Map)
            {
                if (tailwindClass.StartsWith(item.tailwindPrefix))
                {
                    foreach (var (px,scale) in TailwindSpacingScaleMap)
                    {
                        if (tailwindClass == item.tailwindPrefix + scale)
                        {
                            return (item.htmlStyleName, px);
                        }
                    }
                }
            }

            return None;
        }
    }
    
    static Result<string> ConvertToTailwindClass(ProjectConfig project,string name, string value)
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
                    var result = ConvertToTailwindClass(project, name, conditionalValue.left);
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
                        var result = ConvertToTailwindClass(project, name, conditionalValue.right);
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

        // try to handle by spacing scale or arbitrary value
        {
            var maybe = TailwindSpacingScale.Try_Convert_From_HtmlStyle_to_TailwindClass(name, value);
            if (maybe.HasValue)
            {
                return maybe.Value;
            }
        }
        
        var isValueDouble = double.TryParse(value, out var valueAsDouble);

        switch (name)
        {
            
            case "padding":       
            case "padding-right": 
            case "padding-left":  
            case "padding-top":   
            case "padding-bottom":
            
            case "margin":        
            case "margin-right":  
            case "margin-left":   
            case "margin-top":
            case "margin-bottom":
            {
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
                
                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return $"{name}-[{value}]";
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

            case "flex-wrap":
            {
                if (value == "wrap")
                {
                    return "flex-wrap";
                }
                
                if (value == "nowrap")
                {
                    return "flex-nowrap";
                }
                break;
            }

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
                    if (project.Colors.TryGetValue(parts[2], out var htmlColor))
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
                if (project.Colors.TryGetValue(value, out var htmlColor))
                {
                    value = htmlColor;
                }

                return $"text-[{value}]";
            }

            case "border-color":
            {
                if (project.Colors.TryGetValue(value, out var htmlColor))
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

            

            case "border":
            {
                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3)
                {
                    if (project.Colors.TryGetValue(parts[2], out var htmlColor))
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
                if (project.Colors.TryGetValue(value, out var htmlColor))
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
                if (TailwindSpacingScaleMap.TryGetValue(value, out var insetValue))
                {
                    value = insetValue.ToString(CultureInfo_en_US);
                }
                else
                {
                    value = "[" + value + "]";
                }
                
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
            
            case "word-wrap":
            {
                switch (value)
                {
                    case "break-word": return "break-words";
                }
                break;
            }
            
            case "overflow":
            {
                switch (value)
                {
                    case "auto":    return "overflow-auto";
                    case "hidden":  return "overflow-hidden";
                    case "visible": return "overflow-visible";
                    case "scroll":  return "overflow-scroll";
                }
                break;
            }
            
            case "align-self":
            {
                switch (value)
                {
                    case "auto":     return "self-auto";
                    case "start":    return "self-start";
                    case "end":      return "self-end";
                    case "center":   return "self-center";
                    case "stretch":  return "self-stretch";
                    case "baseline": return "self-baseline";
                }
                break;
            }
            
            case "outline-offset":
            {
                switch (value)
                {
                    case "0":   return "outline-offset-0";
                    case "1px": return "outline-offset-1";
                    case "2px": return "outline-offset-2";
                    case "4px": return "outline-offset-4";
                    case "8px": return "outline-offset-8";
                }
                break;
            }
            
            case "flex":
            {
                switch (value)
                {
                    case "1 1 0%":   return "flex-1";
                    case "1 1 0":   return "flex-1";
                    case "0 1 auto": return "flex-initial";
                    case "0 0 auto": return "flex-0";
                    case "auto":     return "flex-auto";
                    case "none":     return "flex-none";
                }
                break;
            }
            
        }

        // todo: more clever
        
        if (name == "outline-offset")
        {
            return "outline-offset-[-1px]";
        }
        
        
        if (name == "flex" && value == "1 1 1")
        {
            return "flex-1";
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

    static HtmlStyle ToHtmlStyle(int projectId, string name, string value)
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
            case "line-height":
            case "outline-offset":
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
            case "flex-wrap":
            case "outline":
            case "text-decoration":
            case "font-style":
            case "word-wrap":
            case "align-self":
            case "flex":
            case "overflow":
            case "align-content":
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
                        if (GetProjectConfig(projectId).Colors.TryGetValue(parts[i], out var color))
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
                if (GetProjectConfig(projectId).Colors.TryGetValue(value, out var realColor))
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

    public static StyleAttribute ParseStyleAttibute(string nameValueCombined)
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

    static Maybe<(string Pseudo, string NewText)> TryReadPseudo(string text)
    {
        foreach (var pseudo in MediaQueries.Keys)
        {
            var prefix = pseudo + ":";
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var newText = text.RemoveFromStart(prefix, StringComparison.OrdinalIgnoreCase);

                return (prefix.RemoveFromEnd(":").ToLower(), newText);
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