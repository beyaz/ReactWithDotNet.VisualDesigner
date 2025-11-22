using ReactWithDotNet.ThirdPartyLibraries.GoogleMaterialSymbols;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BSymbol : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string color { get; set; }

    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string filled { get; set; }

    [Suggestions("16 , 24 , 32 , 36")]
    [JsTypeInfo(JsType.Number)]
    public string size { get; set; }

    [Suggestions("chevron_right")]
    [JsTypeInfo(JsType.String)]
    public string symbol { get; set; }

    [Suggestions("outlined , sharp , rounded")]
    [JsTypeInfo(JsType.String)]
    public string type { get; set; }

    [Suggestions("300 , 400 , 500 , 600 , 700")]
    [JsTypeInfo(JsType.Number)]
    public string weight { get; set; }

    
    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BSymbol))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var import = (nameof(BSymbol),"b-icon");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }
    
    [TryGetIconForElementTreeNode]
    public static Scope TryGetIconForElementTreeNode(Scope scope)
    {
        var model = Plugin.VisualElementModel[scope];

        if (model.Tag == nameof(BDigitalGroupView))
        {
            return Scope.Create(new()
            {
                { Plugin.IconForElementTreeNode, new IconImage() }
            });
        }

        return Scope.Empty;
    }

    protected override Element render()
    {
        return new MaterialSymbol
        {
            name = symbol,

            size = size.HasValue ? int.Parse(size) : null,

            styleVariant = type.HasValue ? Enum.Parse<MaterialSymbolVariant>(type) : MaterialSymbolVariant.outlined,

            color = color,

            weight = weight.HasValue ? int.Parse(weight) : null,

            fill = filled.HasValue ? filled == "true" ? 1 : 0 : null,
            
            id = id,
            onClick = onMouseClick
        };
    }
}