namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BPlateNumber : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string label { get; set; }

    [JsTypeInfo(JsType.String)]
    public string value { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BPlateNumber))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        input = ApplyTranslateOperationOnProps(input, nameof(label));
        
        var import = (nameof(BPlateNumber),"b-plate-number");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }
    
    
    protected override Element render()
    {
        var textContent = string.Empty;
        if (label.HasValue)
        {
            textContent = label;
        }

        if (value.HasValue)
        {
            textContent += " | " + value;
        }

        return new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
        {
            new div { textContent },

            Id(id), OnClick(onMouseClick)
        };
    }
}