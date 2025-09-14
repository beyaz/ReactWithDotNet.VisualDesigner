using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ReactWithDotNet.VisualDesigner.CssDomain;

static class TailwindCss
{
    static readonly ImmutableDictionary<string, double> TailwindSpacingScaleMap = new Dictionary<string, double>
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
    }.ToImmutableDictionary();

    public static Result<string> ConvertDesignerStyleItemToTailwindClassName(ProjectConfig project, string designerStyleItemText)
    {
        string pseudo = null;

        TryReadPseudo(designerStyleItemText).HasValue(x =>
        {
            pseudo = x.Pseudo;

            designerStyleItemText = x.NewText;
        });

        if (pseudo.HasNoValue() && project.Styles.TryGetValue(designerStyleItemText, out _))
        {
            return designerStyleItemText;
        }

        var tailwindClassNames = new List<string>();
        {
            DesignerStyleItem designerStyleItem;
            {
                var result = CreateDesignerStyleItemFromText(project, designerStyleItemText);
                if (result.HasError)
                {
                    return result.Error;
                }

                designerStyleItem = result.Value;
            }

            foreach (var finalCssItem in designerStyleItem.FinalCssItems)
            {
                string tailwindClassName;
                {
                    var result = ConvertToTailwindClass(project, finalCssItem);
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

        static Result<string> ConvertToTailwindClass(ProjectConfig project, FinalCssItem finalCssItem)
        {
            var cssAttributeName = finalCssItem.Name;
            var cssAttributeValue = finalCssItem.Value;

            // check is conditional sample: border-width: {props.isSelected} ? 2 : 5
            {
                var (success, condition, left, right) = TryParseConditionalValue(cssAttributeValue);
                if (success)
                {
                    string lefTailwindClass;
                    {
                        var cssItem = CreateFinalCssItem(cssAttributeName, left);
                        if (cssItem.HasError)
                        {
                            return cssItem.Error;
                        }

                        var result = ConvertToTailwindClass(project, cssItem.Value);
                        if (result.HasError)
                        {
                            return result.Error;
                        }

                        lefTailwindClass = result.Value;
                    }

                    var rightTailwindClass = string.Empty;

                    if (right.HasValue())
                    {
                        var cssItem = CreateFinalCssItem(cssAttributeName, right);
                        if (cssItem.HasError)
                        {
                            return cssItem.Error;
                        }

                        var result = ConvertToTailwindClass(project, cssItem.Value);
                        if (result.HasError)
                        {
                            return result.Error;
                        }

                        rightTailwindClass = result.Value;
                    }

                    return "${" + $"{ClearConnectedValue(condition)} ? \"{lefTailwindClass}\" : \"{rightTailwindClass}\"" + '}';
                }
            }

            // try to handle by spacing scale or arbitrary value
            {
                foreach (var item in TailwindSpacingScale.Try_Convert_From_HtmlStyle_to_TailwindClass(cssAttributeName, cssAttributeValue))
                {
                    return item;
                }
            }

            // TRY TO HANDLE BY PROJECT CONFIG
            {
                foreach (var className in tryConvert_HtmlCssStyle_to_ProjectDefinedCssClass(project, cssAttributeName, cssAttributeValue))
                {
                    return className;
                }
            }

            var isValueDouble = double.TryParse(cssAttributeValue, out var valueAsDouble);

            switch (cssAttributeName)
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
                    cssAttributeName = cssAttributeName switch
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

                        _ => cssAttributeName
                    };

                    if (isValueDouble)
                    {
                        cssAttributeValue = valueAsDouble.AsPixel();
                    }

                    return $"{cssAttributeName}-[{cssAttributeValue}]";
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
                        cssAttributeValue = valueAsDouble.AsPixel();
                    }

                    return $"{cssAttributeName}-[{cssAttributeValue}]";
                }

                case "transform":
                {
                    if (cssAttributeValue.StartsWith("rotate("))
                    {
                        var parts = cssAttributeValue.Split("()".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            var sign = parts[1][0] == '-' ? "-" : "";
                            if (parts[1].EndsWith("deg"))
                            {
                                return $"{sign}rotate-{cssAttributeValue.RemoveFromEnd("deg")}";
                            }
                        }
                    }

                    if (cssAttributeValue == "translateY(50%)")
                    {
                        return "transform translate-y-1/2";
                    }

                    if (cssAttributeValue == "translateY(-50%)")
                    {
                        return "transform -translate-y-1/2";
                    }

                    if (cssAttributeValue == "translateX(50%)")
                    {
                        return "transform translate-x-1/2";
                    }

                    if (cssAttributeValue == "translateX(-50%)")
                    {
                        return "transform -translate-x-1/2";
                    }

                    break;
                }
                case "outline":
                {
                    return $"{cssAttributeName}-{cssAttributeValue}";
                }

                case "text-decoration":
                {
                    return $"{cssAttributeValue}";
                }

                case "text-align":
                {
                    return $"text-{cssAttributeValue}";
                }

                case "width":
                {
                    if (cssAttributeValue == "fit-content")
                    {
                        return "w-fit";
                    }

                    if (cssAttributeValue == "100%")
                    {
                        return "w-full";
                    }

                    return $"w-[{cssAttributeValue}]";
                }

                case "height":
                {
                    if (cssAttributeValue == "fit-content")
                    {
                        return "h-fit";
                    }

                    if (cssAttributeValue == "100%")
                    {
                        return "h-full";
                    }

                    return $"h-[{cssAttributeValue}]";
                }

                case "max-width":
                    return $"max-w-[{cssAttributeValue}]";

                case "max-height":
                    return $"max-h-[{cssAttributeValue}]";

                case "min-height":
                    return $"min-h-[{cssAttributeValue}]";

                case "z-index":
                {
                    if (cssAttributeValue.In("0", "10", "20", "30", "40", "50", "auto"))
                    {
                        return $"z-{cssAttributeValue}";
                    }

                    return $"z-[{cssAttributeValue}]";
                }

                case "overflow-y":
                case "overflow-x":
                    return $"{cssAttributeName}-{cssAttributeValue}";

                case "border-top-left-radius":
                    return $"rounded-tl-[{cssAttributeValue}]";

                case "border-top-right-radius":
                    return $"rounded-tr-[{cssAttributeValue}]";

                case "border-bottom-left-radius":
                    return $"rounded-bl-[{cssAttributeValue}]";

                case "border-bottom-right-radius":
                    return $"rounded-br-[{cssAttributeValue}]";

                case "flex-grow":
                    return cssAttributeValue switch
                    {
                        "0" => "grow-0",
                        "1" => "grow",
                        _   => $"flex-grow-[{cssAttributeValue}]"
                    };
                case "flex-shrink":
                    return cssAttributeValue switch
                    {
                        "0" => "flex-shrink-0",
                        "1" => "flex-shrink-1",
                        _   => $"flex-shrink-[{cssAttributeValue}]"
                    };

                case "flex-wrap":
                {
                    if (cssAttributeValue == "wrap")
                    {
                        return "flex-wrap";
                    }

                    if (cssAttributeValue == "nowrap")
                    {
                        return "flex-nowrap";
                    }

                    break;
                }

                case "border-bottom-width":
                    return $"border-b-[{cssAttributeValue}]";

                case "border-top-width":
                    return $"border-t-[{cssAttributeValue}]";

                case "border-left-width":
                    return $"border-l-[{cssAttributeValue}]";

                case "border-right-width":
                    return $"border-r-[{cssAttributeValue}]";

                case "border-top":
                case "border-right":
                case "border-left":
                case "border-bottom":
                {
                    var direction = cssAttributeName.Split('-', StringSplitOptions.RemoveEmptyEntries).Last();

                    var parts = cssAttributeValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
                    return $"{cssAttributeValue}";

                case "color":
                {
                    return $"text-[{cssAttributeValue}]";
                }

                case "border-color":
                {
                    var isNamedColor = project.Colors.ContainsKey(cssAttributeValue);
                    if (isNamedColor)
                    {
                        return $"border-{cssAttributeValue}";
                    }

                    return $"border-[{cssAttributeValue}]";
                }

                case "gap":
                    return $"gap-[{cssAttributeValue}]";

                case "size":
                    return $"size-[{cssAttributeValue}]";

                case "bottom":
                case "top":
                case "left":
                case "right":
                    return $"{cssAttributeName}-[{cssAttributeValue}]";

                case "flex-direction":
                {
                    if (cssAttributeValue == "column")
                    {
                        return "flex-col";
                    }

                    if (cssAttributeValue == "row")
                    {
                        return "flex";
                    }

                    break;
                }

                case "align-items":
                    return $"items-{cssAttributeValue.RemoveFromStart("align-")}";

                case "justify-content":
                    return $"justify-{cssAttributeValue.Split('-').Last()}";

                case "border-radius":
                    return $"rounded-[{cssAttributeValue}]";

                case "font-size":
                    return $"[font-size:{cssAttributeValue}]";

                case "border-width":
                {
                    if (isValueDouble)
                    {
                        cssAttributeValue = valueAsDouble.AsPixel();
                    }

                    return $"border-[{cssAttributeValue}]";
                }

                case "border":
                {
                    foreach (var (width, style, color) in TryParseBorderCss(cssAttributeValue))
                    {
                        var (isNamedColor, namedColor) = tryResolveColorName(project, color);

                        var items = new List<string>();
                        if (width == "1px")
                        {
                            items.Add("border");
                        }
                        else
                        {
                            items.Add($"border-[{width}]");
                        }

                        if (style != "solid")
                        {
                            items.Add($"border-{style}");
                        }

                        if (isNamedColor)
                        {
                            items.Add($"border-{namedColor}");
                        }
                        else
                        {
                            items.Add($"border-[{color}]");
                        }

                        return string.Join(" ", items);
                    }

                    break;
                }

                case "background-color":
                case "background":
                case "bg":
                {
                    var (isNamedColor, namedColor) = tryResolveColorName(project, cssAttributeValue);
                    if (isNamedColor)
                    {
                        return $"bg-{namedColor}";
                    }

                    return $"bg-[{cssAttributeValue.Replace(" ", "")}]";
                }
                case "position":
                    return $"{cssAttributeValue}";

                case "border-style":
                {
                    return $"border-{cssAttributeValue}";
                }

                case "cursor":
                {
                    return $"cursor-{cssAttributeValue}";
                }

                case "user-select":
                {
                    if (cssAttributeValue == "none")
                    {
                        return "select-none";
                    }

                    break;
                }

                case "inset":
                {
                    if (TailwindSpacingScaleMap.TryGetValue(cssAttributeValue, out var insetValue))
                    {
                        cssAttributeValue = insetValue.ToString(CultureInfo_en_US);
                    }
                    else
                    {
                        cssAttributeValue = "[" + cssAttributeValue + "]";
                    }

                    return $"inset-{cssAttributeValue}";
                }

                case "font-family":
                {
                    return $"font-[{cssAttributeValue}]";
                }
                case "font-style":
                {
                    return $"[font-style:{cssAttributeValue}]";
                }
                case "font-weight":
                {
                    return $"[font-weight:{cssAttributeValue}]";
                }
                case "line-height":
                {
                    return $"[line-height:{cssAttributeValue}]";
                }

                case "word-wrap":
                {
                    switch (cssAttributeValue)
                    {
                        case "break-word": return "break-words";
                    }

                    break;
                }

                case "overflow":
                {
                    switch (cssAttributeValue)
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
                    switch (cssAttributeValue)
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
                    switch (cssAttributeValue)
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
                    switch (cssAttributeValue)
                    {
                        case "1":        return "flex-1";
                        case "1 1 0%":   return "flex-1";
                        case "1 1 0":    return "flex-1";
                        case "0 1 auto": return "flex-initial";
                        case "0 0 auto": return "flex-0";
                        case "auto":     return "flex-auto";
                        case "none":     return "flex-none";
                    }

                    break;
                }

                case "grid-template-columns":
                {
                    return TailwindGrid.ResolveGridCols(cssAttributeValue);
                }

                case "grid-template-rows":
                {
                    return TailwindGrid.ResolveGridRows(cssAttributeValue);
                }

                case "page-break-after":
                {
                    switch (cssAttributeValue)
                    {
                        case "auto":       return "break-after-auto";
                        case "avoid":      return "break-after-avoid";
                        case "always":     return "break-after-page";
                        case "left":       return "break-after-left";
                        case "right":      return "break-after-right";
                        case "page":       return "break-after-page";
                        case "column":     return "break-after-column";
                        case "avoid-page": return "break-after-avoid-page";
                        case "all":        return "break-after-all";
                    }

                    break;
                }
            }

            if (cssAttributeName == "outline-offset")
            {
                if (cssAttributeValue == "-1px")
                {
                    return "outline-offset-[-1px]";
                }
            }

            return new InvalidOperationException($"Css not handled. {cssAttributeName}: {cssAttributeValue}");

            static Maybe<string> tryConvert_HtmlCssStyle_to_ProjectDefinedCssClass(ProjectConfig project, string cssAttributeName, string cssAttributeValue)
            {
                return Cache.AccessValue($"{nameof(tryConvert_HtmlCssStyle_to_ProjectDefinedCssClass)}-{project.Name}-{cssAttributeName}-{cssAttributeValue}", () =>
                {
                    foreach (var (key, value) in project.Styles)
                    {
                        if (value == $"{cssAttributeName}: {cssAttributeValue};")
                        {
                            return (Maybe<string>)key;
                        }
                    }

                    return None;
                });
            }

            static (bool isNamedColor, string namedColor) tryResolveColorName(ProjectConfig project, string color)
            {
                bool isNamedColor;
                string namedColor = null;
                {
                    isNamedColor = project.Colors.TryGetValue(color, out _);
                    if (isNamedColor)
                    {
                        namedColor = color;
                    }
                    else
                    {
                        foreach (var (name, htmlColor) in project.Colors)
                        {
                            if (htmlColor == color)
                            {
                                isNamedColor = true;
                                namedColor   = name;
                                break;
                            }
                        }
                    }
                }

                return (isNamedColor, namedColor);
            }
        }
    }

    public static Result<DesignerStyleItem> TryConvertTailwindUtilityClassToHtmlStyle(ProjectConfig project, string utilityCssClassName)
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
        {
            if (project.Styles.TryGetValue(utilityCssClassName, out var cssText))
            {
                var (map, exception) = Style.ParseCssAsDictionary(cssText);
                if (exception is null)
                {
                    return CreateDesignerStyleItem(new()
                    {
                        Pseudo        = pseudo,
                        FinalCssItems = from x in map select CreateFinalCssItem(x)
                    });
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

                return CreateDesignerStyleItem(new()
                {
                    Pseudo        = pseudo,
                    FinalCssItems = [CreateFinalCssItem(styleName, numberSuffix.Value * 4 + "px")]
                });
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
                return CreateDesignerStyleItem(new()
                {
                    Pseudo        = pseudo,
                    FinalCssItems = [CreateFinalCssItem("font-weight", weightAsNumber)]
                });
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
                return CreateDesignerStyleItem(new()
                {
                    Pseudo        = pseudo,
                    FinalCssItems = [CreateFinalCssItem("font-family", value)]
                });
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
                    return CreateDesignerStyleItem(new()
                    {
                        Pseudo        = pseudo,
                        FinalCssItems = [CreateFinalCssItem("color", tailwindColor.Value)]
                    });
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
                    return CreateDesignerStyleItem(new()
                    {
                        Pseudo        = pseudo,
                        FinalCssItems = [CreateFinalCssItem("background", tailwindColor.Value)]
                    });
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
                return CreateDesignerStyleItem(new()
                {
                    Pseudo        = pseudo,
                    FinalCssItems = [CreateFinalCssItem("text-decoration-line", value)]
                });
            }
        }

        // try read from project config
        {
            var cssItem = ParseStyleAttribute(utilityCssClassName);
            if (cssItem.Name == "color" && cssItem.Value is not null && project.Colors.TryGetValue(cssItem.Value, out var realColor))
            {
                return CreateDesignerStyleItem(new()
                {
                    Pseudo        = pseudo,
                    FinalCssItems = [CreateFinalCssItem("color", realColor)]
                });
            }
        }

        // try to handle by spacing scale or arbitrary value
        {
            foreach (var (htmlStyleName, htmlStyleValue) in TailwindSpacingScale.Try_Convert_From_TailwindClass_to_HtmlStyle(utilityCssClassName))
            {
                return CreateDesignerStyleItem(new()
                {
                    Pseudo        = pseudo,
                    FinalCssItems = [CreateFinalCssItem(htmlStyleName, htmlStyleValue)]
                });
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
                    return CreateDesignerStyleItem(new()
                    {
                        Pseudo        = pseudo,
                        FinalCssItems = [CreateFinalCssItem(styleName, arbitrary.Value)]
                    });
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

            foreach (var (tailwindPrefix, styleName) in map)
            {
                var arbitrary = tryGetArbitraryValue(utilityCssClassName, tailwindPrefix);
                if (arbitrary.HasValue)
                {
                    var color = arbitrary.Value;
                    if (project.Colors.TryGetValue(color, out var realColor))
                    {
                        color = realColor;
                    }

                    return CreateDesignerStyleItem(new()
                    {
                        Pseudo        = pseudo,
                        FinalCssItems = [CreateFinalCssItem(styleName, color)]
                    });
                }
            }
        }

        // cursor-
        {
            if (utilityCssClassName.StartsWith("cursor-"))
            {
                return CreateDesignerStyleItem(new()
                {
                    Pseudo        = pseudo,
                    FinalCssItems = [CreateFinalCssItem("cursor", utilityCssClassName.RemoveFromStart("cursor-"))]
                });
            }
        }

        return new ArgumentOutOfRangeException($"{utilityCssClassName} is not a valid Tailwind class.");

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
            var pattern = $@"^{Regex.Escape(prefix)}-\[(.+?)\]$";
            var match = Regex.Match(input, pattern);

            return match.Success ? match.Groups[1].Value : None;
        }
    }

    static class TailwindGrid
    {
        static readonly Dictionary<string, string> PredefinedColumns = new()
        {
            { "1fr", "grid-cols-1" },
            { "1fr 1fr", "grid-cols-2" },
            { "1fr 1fr 1fr", "grid-cols-3" },
            { "1fr 1fr 1fr 1fr", "grid-cols-4" },
            { "1fr 1fr 1fr 1fr 1fr", "grid-cols-5" },
            { "1fr 1fr 1fr 1fr 1fr 1fr", "grid-cols-6" },
            { "1fr 1fr 1fr 1fr 1fr 1fr 1fr", "grid-cols-7" },
            { "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr", "grid-cols-8" },
            { "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr", "grid-cols-9" },
            { "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr", "grid-cols-10" },
            { "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr", "grid-cols-11" },
            { "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr", "grid-cols-12" }
        };

        static readonly Dictionary<string, string> PredefinedRows = new()
        {
            { "1fr", "grid-rows-1" },
            { "1fr 1fr", "grid-rows-2" },
            { "1fr 1fr 1fr", "grid-rows-3" },
            { "1fr 1fr 1fr 1fr", "grid-rows-4" },
            { "1fr 1fr 1fr 1fr 1fr", "grid-rows-5" },
            { "1fr 1fr 1fr 1fr 1fr 1fr", "grid-rows-6" }
        };

        public static Result<string> ResolveGridCols(string cssValue)
        {
            if (string.IsNullOrWhiteSpace(cssValue))
            {
                return new ArgumentNullException(nameof(cssValue));
            }

            if (PredefinedColumns.TryGetValue(cssValue, out var predefinedClass))
            {
                return predefinedClass;
            }

            var parts = cssValue.Split([' '], StringSplitOptions.RemoveEmptyEntries);

            return $"grid-cols-[{string.Join("_", parts)}]";
        }

        public static Result<string> ResolveGridRows(string cssValue)
        {
            if (string.IsNullOrWhiteSpace(cssValue))
            {
                return new ArgumentNullException(nameof(cssValue));
            }

            if (PredefinedRows.TryGetValue(cssValue, out var predefinedClass))
            {
                return predefinedClass;
            }

            var parts = cssValue.Split([' '], StringSplitOptions.RemoveEmptyEntries);

            return $"grid-rows-[{string.Join("_", parts)}]";
        }
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
            // padding: 6px 12px => py-1.5 px-3
            // margin: 6px 12px => my-1.5 mx-3
            foreach (var (htmlStyleName, tailwindPrefix) in new[] { (htmlStyleName: "padding", tailwindPrefix: "p"), (htmlStyleName: "margin", tailwindPrefix: "m") })
            {
                if (name == htmlStyleName)
                {
                    if (value.Contains(' '))
                    {
                        var parts = value.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            foreach (var y in try_Convert_PixelValue_To_SpacingScale_Or_ArbitraryValue(parts[0]))
                            {
                                foreach (var x in try_Convert_PixelValue_To_SpacingScale_Or_ArbitraryValue(parts[1]))
                                {
                                    return $"{tailwindPrefix}y-{y} {tailwindPrefix}x-{x}";
                                }
                            }
                        }
                    }
                }
            }

            foreach (var (htmlStyleName, tailwindPrefix) in Html_to_TailwindName_Map)
            {
                if (htmlStyleName == name)
                {
                    var maybe = try_Convert_PixelValue_To_SpacingScale_Or_ArbitraryValue(value);
                    if (maybe.HasValue)
                    {
                        return tailwindPrefix + maybe.Value;
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
            foreach (var (htmlStyleName, tailwindPrefix) in Html_to_TailwindName_Map)
            {
                if (tailwindClass.StartsWith(tailwindPrefix))
                {
                    foreach (var (px, scale) in TailwindSpacingScaleMap)
                    {
                        if (tailwindClass == tailwindPrefix + scale)
                        {
                            return (htmlStyleName, px);
                        }
                    }
                }
            }

            return None;
        }
    }
}