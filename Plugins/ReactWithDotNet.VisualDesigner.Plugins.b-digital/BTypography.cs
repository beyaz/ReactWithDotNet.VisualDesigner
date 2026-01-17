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
        
        input = ApplyTranslateOperationOnProps(input, Design.Content);
        
        var import = (nameof(BTypography),"b-core-typography");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }
    
    
    static readonly Dictionary<string, StyleModifier> ColorMap = new()
    {
        { "primary", new Style { Color("#16A085") } },
        { "textPrimary", new Style { Color(rgba(0, 0, 0, 0.87)) } },
        
        { "secondary", new Style { Color("#FF9500") } },
        { "textSecondary", new Style { Color(rgba(0, 0, 0, 0.6)) } }
    };

    
    static readonly Dictionary<string, StyleModifier> VariantMap = new()
    {
        { "body0", new Style { FontSize18, FontWeight400, LineHeight(1.5), Font("Roboto, sans-serif") } },
        { "body0m", new Style { FontSize18, FontWeight500, LineHeight(1.5),Font("Roboto, sans-serif") } },
        { "body0b", new Style { FontSize18, FontWeight600, LineHeight(1.5),Font("Roboto, sans-serif") } },
        
        
        { "body1", new Style { FontSize16, FontWeight400, LineHeight(1.5), Font("Roboto, sans-serif") } },
        { "body1m", new Style { FontSize16, FontWeight500, LineHeight(1.5),Font("Roboto, sans-serif") } },
        { "body1b", new Style { FontSize16, FontWeight600, LineHeight(1.5),Font("Roboto, sans-serif") } },
       
        { "body2", new Style { FontSize14, FontWeight400, LineHeight(1.43),Font("Roboto, sans-serif") } },
        { "body2m", new Style { FontSize14, FontWeight500, LineHeight(1.5),Font("Roboto, sans-serif") } },
        { "body2b", new Style { FontSize14, FontWeight600, LineHeight(1.5),Font("Roboto, sans-serif") } },
        
        { "body3", new Style { FontSize20, FontWeight400, LineHeight(1.5),Font("Roboto, sans-serif") } },
        { "body3m", new Style { FontSize20, FontWeight500, LineHeight(1.5),Font("Roboto, sans-serif") } },
        { "body3b", new Style { FontSize20, FontWeight600, LineHeight(1.5),Font("Roboto, sans-serif") } },
     
        { "body4", new Style { FontSize24, FontWeight400, LineHeight(1.5),Font("Roboto, sans-serif") } },
        { "body4m", new Style { FontSize24, FontWeight500, LineHeight(1.5),Font("Roboto, sans-serif") } },
        { "body4b", new Style { FontSize24, FontWeight600, LineHeight(1.5),Font("Roboto, sans-serif") } },

      
        
        { "subtitle1", new Style { FontSize("1rem"), FontWeight400, LineHeight(1.75), Font("Roboto, sans-serif") } },
        { "subtitle2", new Style { FontSize("0.875rem"), FontWeight500, LineHeight(1.57), Font("Roboto, sans-serif") } },
        
        { "h1", new Style { FontSize("6rem"), FontWeight300, LineHeight(1.167), Font("Roboto, sans-serif") } },
        { "h2", new Style { FontSize("3.75rem"), FontWeight300, LineHeight(1.2) ,Font("Roboto, sans-serif")} },
        { "h3", new Style { FontSize("3rem"), FontWeight400, LineHeight(1.167) ,Font("Roboto, sans-serif")} },
        { "h4", new Style { FontSize("2.125rem"), FontWeight400, LineHeight(1.235) ,Font("Roboto, sans-serif")} },
        { "h5", new Style { FontSize("1.5rem"), FontWeight400, LineHeight(1.334) ,Font("Roboto, sans-serif")} },
        { "h6", new Style { FontSize("1.25rem"), FontWeight500, LineHeight(1.6) ,Font("Roboto, sans-serif")} },
        
        { "caption", new Style { FontSize("0.75rem"), FontWeight400, LineHeight(1.66) ,Font("Roboto, sans-serif")} },
        
        { "button", new Style { FontSize("0.875rem"), FontWeight500, LineHeight(1.75) ,Font("Roboto, sans-serif") , TextTransformUpperCase} },
       
        { "minibutton", new Style { FontSize16, FontWeight400, LineHeight(1.5), Font("Roboto, sans-serif") } },
        
        { "overline", new Style { FontSize("0.75rem"), FontWeight400, LineHeight(2.66) ,Font("Roboto, sans-serif"),TextTransformUpperCase} }
    };

    [Suggestions("primary , secondary , textPrimary , textSecondary")]
    [JsTypeInfo(JsType.String)]
    public string color { get; set; }

    public string dangerouslySetInnerHTML { get; set; }

    [Suggestions("body0 , body0m , body0b , body1 , body1m , body1b , body2 , body2m , body2b , body3 , body3m , body3b , body4 , body4m , body4b , subtitle1 , subtitle2 , caption , button , minibutton , overline , h1 , h2 , h3 , h4 , h5 , h6")]
    [JsTypeInfo(JsType.String)]
    public string variant { get; set; }

    protected override Element render()
    {
        var styleOverride = new Style();

        if (variant.HasValue)
        {
            if (VariantMap.TryGetValue(variant, out var value))
            {
                styleOverride += value;
            }
        }

        if (color.HasValue)
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