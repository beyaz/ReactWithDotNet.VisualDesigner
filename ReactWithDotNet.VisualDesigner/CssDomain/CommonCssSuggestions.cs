namespace ReactWithDotNet.VisualDesigner;

public sealed record CommonCssSuggestions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map = new Dictionary<string, IReadOnlyList<string>>
    {
        ["width"]           = new List<string> { "auto", "fit-content", "max-content", "min-content", "inherit", "initial", "unset", "100%", "75%", "50%", "25%" },
        ["height"]          = new List<string> { "auto", "fit-content", "max-content", "min-content", "inherit", "initial", "unset", "100%", "75%", "50%", "25%" },
        ["display"]         = new List<string> { "flex", "grid","inline-flex" },
        ["position"]        = new List<string> { "static", "relative", "absolute", "fixed", "sticky", "inherit", "initial", "unset" },
        ["flex-direction"]  = new List<string> { "row", "row-reverse", "column", "column-reverse", "inherit", "initial", "unset" },
        ["justify-content"] = new List<string> { "flex-start", "flex-end", "center", "space-between", "space-around", "space-evenly", "inherit", "initial", "unset" },
        ["align-items"]     = new List<string> { "stretch", "flex-start", "flex-end", "center", "baseline", "inherit", "initial", "unset" },
        ["overflow"]        = new List<string> { "visible", "hidden", "scroll", "auto", "clip", "inherit", "initial", "unset" },
        ["overflow-x"]      = new List<string> { "visible", "hidden", "scroll", "auto", "clip", "inherit", "initial", "unset" },
        ["overflow-y"]      = new List<string> { "visible", "hidden", "scroll", "auto", "clip", "inherit", "initial", "unset" },
        ["visibility"]      = new List<string> { "visible", "hidden", "collapse", "inherit", "initial", "unset" },
        ["opacity"]         = new List<string> { "0", "0.1", "0.5", "1", "inherit", "initial", "unset" },
        ["cursor"]          = new List<string> { "auto", "default", "pointer", "wait", "text", "move", "not-allowed", "crosshair", "inherit", "initial", "unset" },
        ["font-size"]       = new List<string> { "small", "medium", "large", "inherit", "initial", "unset", "16px", "1.2rem" },
        ["font-weight"]     = new List<string> { "normal", "bold", "bolder", "lighter", "100", "200", "300", "inherit", "initial", "unset" },
        ["line-height"]     = new List<string> { "normal", "inherit", "initial", "unset", "1.5", "2" },
        ["text-align"]      = new List<string> { "left", "right", "center", "justify", "inherit", "initial", "unset" },
        ["text-decoration"] = new List<string> { "none", "underline", "overline", "line-through", "inherit", "initial", "unset" },
        ["white-space"]     = new List<string> { "normal", "nowrap", "pre", "pre-wrap", "pre-line", "inherit", "initial", "unset" },
        ["pointer-events"]  = new List<string> { "auto", "none", "inherit", "initial", "unset" },
        ["object-fit"]      = new List<string> { "cover", "contain", "fill", "none", "scale-down" },
        ["border-width"]    = new List<string> { "1px", "1.5px", "2px", "4px" },
        ["border-style"]    = new List<string> { "solid", "dashed", "dottet" },
        ["align-self"]      =["stretch"],
        ["word-wrap"]       =["break-word"],
        
        ["transform"]   = new List<string> { "translateY(-50%)", "translateX(-50%)","rotate(-180deg)" },
        ["flex-wrap"]   = new List<string> { "nowrap", "wrap", "wrap-reverse", "inherit", "initial", "unset" },
        ["min-width"]   = new List<string> { "100px" },
        ["min-height"]  = new List<string> { "100px" },
        ["flex"]        = new List<string> { "1 1 0" },
        ["outline"]     = new List<string> { "none" },
        ["user-select"] = new List<string> { "none" },
        ["grid-template-columns"] = new List<string>
        {
            "1fr", "1fr 1fr", "1fr 1fr 1fr", "1fr 1fr 1fr 1fr", "1fr 1fr 1fr 1fr 1fr",
            "1fr 1fr 1fr 1fr 1fr 1fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr",
            "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr",
            "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr",
            "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr"
        },
        ["grid-template-rows"] = new List<string>
        {
            "1fr", "1fr 1fr", "1fr 1fr 1fr", "1fr 1fr 1fr 1fr", "1fr 1fr 1fr 1fr 1fr",
            "1fr 1fr 1fr 1fr 1fr 1fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr",
            "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr",
            "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr",
            "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr"
        }
    };
}