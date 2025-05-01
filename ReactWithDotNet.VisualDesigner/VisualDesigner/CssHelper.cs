namespace ReactWithDotNet.VisualDesigner;

public static class CssHelper
{
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