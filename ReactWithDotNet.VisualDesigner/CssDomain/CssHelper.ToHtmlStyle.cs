namespace ReactWithDotNet.VisualDesigner.CssDomain;

partial class CssHelper
{
    static Result<FinalCssItem> ToHtmlStyle(ProjectConfig project, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);

        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        switch (name)
        {
            // AS SAME
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
            case "transform":
            {
                return CreateFinalCssItem(name, value);
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

                return CreateFinalCssItem(name, value);
            }

            // c o l o r s
            case "background-color":
            case "background":
            case "color":
            case "border-top-color":
            case "border-bottom-color":
            case "border-left-color":
            case "border-right-color":
            {
                return CreateFinalCssItem(name, resolveColor(value));
            }
        }

        return new Exception($"{name}: {value} is not recognized");

        string resolveColor(string val)
        {
            return project.Colors.GetValueOrDefault(val)
                   ??
                   TryGetTailwindColorByName(val)
                   ?? val;
        }
    }
}