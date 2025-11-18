using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalBox : PluginComponentBase
{
    [Suggestions("noMargin, primary, secondary , info ")]
    [JsTypeInfo(JsType.String)]
    public string styleContext { get; set; }
    
    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalBox))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var import = (nameof(BDigitalBox),"b-digital-box");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }
    
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