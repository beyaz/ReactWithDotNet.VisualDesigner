namespace ReactWithDotNet.VisualDesigner;

partial class CssHelper
{
    static Result<IReadOnlyDictionary<string, string>> ToHtmlStyle(ProjectConfig project, string name, string value)
    {
       

        ArgumentNullException.ThrowIfNull(name);

        ArgumentNullException.ThrowIfNull(value);

        value = value.Trim();
        
        ArgumentNullException.ThrowIfNull(value);

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

                return asDictionary((name, value));
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
            case "opacity":
            case "box-shadow":
            case "white-space":
            case "visibility":
            case "user-select":
            case "flex-shrink":
            case "grid-template-columns":
            case "grid-template-rows":
            case "grid-row":
            case "grid-column":
            case "grid-area":
            case "letter-spacing":
            case "page-break-after":
            case "backdrop-filter":
            case "transition":
            case "background-image":
            case "break-after":
            case "box-sizing":
            case "pointer-events":
            {
                return asDictionary((name, value));
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
                    parts[2] = resolveColor(parts[2]);
                    
                    value = string.Join(" ", parts);
                }

                return asDictionary((name, value));
            }

            // m u l t i p l e
            case "px":
            {
                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return asDictionary(("padding-left", value), ("padding-right", value));
            }
            case "py":
            {
                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return asDictionary(("padding-top", value), ("padding-bottom", value));
            }
            case "size":
            {
                if (isValueDouble)
                {
                    value = valueAsDouble.AsPixel();
                }

                return asDictionary(("width", value), ("height", value));
            }

            // c o l o r s
            case "background-color":
            case "background":
            case "color":
            case "border-top-color":
            {
                return asDictionary((name, resolveColor(value)));
            }

            // SPECIAL
            case "transform":
            {
                if (isValueDouble)
                {
                    value = valueAsDouble + "deg";
                }

                return asDictionary((name, value));
            }
        }

        return new Exception($"{name}: {value} is not recognized");

       

        static Dictionary<string, string> asDictionary(params (string Name, string Value)[] items)
        {
            return items.ToDictionary(x => x.Name, x => x.Value);
        }
        
        string resolveColor(string val)
        {
            return project.Colors.GetValueOrDefault(val) ?? TryGetTailwindColorByName(val) ?? val;
        }
    }
}