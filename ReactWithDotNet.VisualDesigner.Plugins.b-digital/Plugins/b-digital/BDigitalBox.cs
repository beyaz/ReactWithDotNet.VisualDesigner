using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BDigitalBox", Package = "b-digital-box")]
sealed class BDigitalBox : PluginComponentBase
{
    [Suggestions("noMargin, primary")]
    [JsTypeInfo(JsType.String)]
    public string styleContext { get; set; }

    protected override Element render()
    {
        var style = new Style();

        if (styleContext == "primary")
        {
            style = new()
            {
                Background(rgb(255, 255, 255)),
                Border(1, solid, rgba(0, 0, 0, 0.12)),
                BorderRadius(8)
            };
        }

        if (styleContext == "noMargin")
        {
            style = new()
            {
                Margin(0)
            };
        }

        return new Grid
        {
            children = { children },
            style    = { style }
        };
    }
}