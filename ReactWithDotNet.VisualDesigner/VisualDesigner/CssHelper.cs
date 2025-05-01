namespace ReactWithDotNet.VisualDesigner;

public static class CssHelper
{
    public static Result<(string Name, string Value)[]> TryConvertCssUtilityClassToHtmlStyle(string utilityCssClassName)
    {
        switch (utilityCssClassName)
        {
            case "w-full":
            {
                return new[]{("width", "100%")};
            }
            
            case "w-fit":
            {
                return new[]{("width", "fit-content")};
            }
            case "h-fit":
            {
                return new[]{("height", "fit-content")};
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
                    ("flexDirection", "row"),
                    ("justifyContent", "center"),
                    ("alignItems", "center")
                };
            }
            case "flex-col-centered":
            {
                return new[]
                {
                    ("display", "flex"),
                    ("flexDirection", "column"),
                    ("justifyContent", "center"),
                    ("alignItems", "center")
                };
                
            }
            case "col":
            {
                return new[]
                {
                    ("display", "flex"),
                    ("flexDirection", "column")
                };
            }
            case "row":
            {
                return new[]
                {
                    ("display", "flex"),
                    ("flexDirection", "row")
                };
            }
        }
        
        return new ArgumentException($"Unknown utility CSS class: {utilityCssClassName}.");

    }
}