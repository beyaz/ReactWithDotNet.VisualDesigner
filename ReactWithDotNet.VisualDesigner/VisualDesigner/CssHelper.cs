
namespace ReactWithDotNet.VisualDesigner;

public static class CssHelper
{
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
        }

        return new Exception($"{name}: {value} is not recognized");
    }

    public static Result<(string Name, string Value)[]> TryConvertCssUtilityClassToHtmlStyle(string utilityCssClassName)
    {
        switch (utilityCssClassName)
        {
            case "w-full":
            {
                return new[]
                {
                    ("width", "100%")
                };
            }

            case "w-fit":
            {
                return new[]
                {
                    ("width", "fit-content")
                };
            }
            case "h-full":
            {
                return new[]
                {
                    ("height", "100%")
                };
            }
            case "h-fit":
            {
                return new[]
                {
                    ("height", "fit-content")
                };
            }
            case "size-fit":
            {
                return new[]
                {
                    ("width", "fit-content"),
                    ("height", "fit-content")
                };
            }

            case "flex-row-centered":
            {
                return new[]
                {
                    ("display", "flex"),
                    ("flex-direction", "row"),
                    ("justify-content", "center"),
                    ("align-items", "center")
                };
            }
            case "flex-col-centered":
            {
                return new[]
                {
                    ("display", "flex"),
                    ("flex-direction", "column"),
                    ("justify-content", "center"),
                    ("align-items", "center")
                };
            }
            case "col":
            {
                return new[]
                {
                    ("display", "flex"),
                    ("flex-direction", "column")
                };
            }
            case "row":
            {
                return new[]
                {
                    ("display", "flex"),
                    ("flex-direction", "row")
                };
            }
        }

        return new ArgumentException($"Unknown utility CSS class: {utilityCssClassName}.");
    }
}