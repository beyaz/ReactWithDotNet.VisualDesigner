using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BTypography : PluginComponentBase
{
    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BTypography))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var import = (nameof(BTypography),"b-core-typography");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }
    
    
    static readonly Dictionary<string, StyleModifier> ColorMap = new()
    {
        { "primary", new Style { Color("#16A085") } },
        { "secondary", new Style { Color("#FF9500") } }
    };

    static readonly Dictionary<string, StyleModifier> VariantMap = new()
    {
        { "body0", new Style { FontSize18, FontWeight400, LineHeight(1.5) } },
        { "body1", new Style { FontSize16, FontWeight400, LineHeight(1.5) } },
        { "body2", new Style { FontSize14, FontWeight400, LineHeight(1.43) } },
        { "body2b", new Style { FontSize14, FontWeight600, LineHeight(1.5) } },
        { "body2m", new Style { FontSize14, FontWeight500, LineHeight(1.5) } },

        { "h1", new Style { FontSize("6rem"), FontWeight300, LineHeight(1.167) } },
        { "h2", new Style { FontSize("3.75rem"), FontWeight300, LineHeight(1.2) } },
        { "h3", new Style { FontSize("3rem"), FontWeight400, LineHeight(1.167) } }
    };

    [Suggestions("primary, secondary")]
    [JsTypeInfo(JsType.String)]
    public string color { get; set; }

    public string dangerouslySetInnerHTML { get; set; }

    [Suggestions("h1, h2 , h3 , h4 , h5 , h6 , body0 , body1 , body2, body2m")]
    [JsTypeInfo(JsType.String)]
    public string variant { get; set; }

    protected override Element render()
    {
        var styleOverride = new Style();

        if (variant.HasValue())
        {
            if (VariantMap.TryGetValue(variant, out var value))
            {
                styleOverride += value;
            }
        }

        if (color.HasValue())
        {
            if (ColorMap.TryGetValue(color, out var value))
            {
                styleOverride += value;
            }
        }

        return new Typography
        {
            children = { children },
            variant  = variant,
            color    = color,
            style    = { styleOverride },

            id      = id,
            onClick = onMouseClick
            //TODO: Open: dangerouslySetInnerHTML = dangerouslySetInnerHTML
        };
    }
}