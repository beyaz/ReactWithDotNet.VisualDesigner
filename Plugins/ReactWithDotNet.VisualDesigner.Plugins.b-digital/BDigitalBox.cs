using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[TsImport(Name = nameof(BDigitalBox), Package = "b-digital-box")]
sealed class BDigitalBox : PluginComponentBase
{
    [Suggestions("noMargin, primary, secondary , info ")]
    [JsTypeInfo(JsType.String)]
    public string styleContext { get; set; }
    
    protected override Element render()
    {
        var style = new Style();

        if (styleContext == "primary")
        {
            style =
            [
                Background(rgb(255, 255, 255)),
                Border(1, solid, rgba(0, 0, 0, 0.12)),
                BorderRadius(8),
                MarginBottom(3 * 8)
            ];
        }
        else if (styleContext == "secondary")
        {
            style = [MarginBottom(3 * 8)];
        }
        else if (styleContext == "info")
        {
            style =
            [
                Background(Gray50),
                BorderRadius(8),
                MarginBottom(3 * 8)
            ];
        }
        else if (styleContext == "noMargin")
        {
            style = [Margin(0)];
        }
        else
        {
            style = [MarginBottom(3 * 8)];
        }
        
        return new Grid
        {
            id = id,
            onClick = onMouseClick,
            
            children = { children },
            style    = { style }
        };
    }
}