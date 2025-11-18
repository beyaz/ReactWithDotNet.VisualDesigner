namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalPlateNumber : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string label { get; set; }

    [JsTypeInfo(JsType.String)]
    public string value { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalPlateNumber))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var import = (nameof(BDigitalPlateNumber),"b-plate-number");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }
    
    protected override Element render()
    {
        return new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
        {
            new div { value ?? "?" }
        };
    }
}