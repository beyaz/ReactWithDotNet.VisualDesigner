namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BCheckBox : PluginComponentBase
{
    [Suggestions("true")]
    [JsTypeInfo(JsType.Boolean)]
    public string disabled { get; set; }
    
    [JsTypeInfo(JsType.Boolean)]
    public string @checked { get; set; }

    [JsTypeInfo(JsType.String)]
    public new string id { get; set; }

    [JsTypeInfo(JsType.String)]
    public string label { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onCheck { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BCheckBox))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        
        input = ApplyTranslateOperationOnProps(input, nameof(label));
        
        
        var node = input.Node;
        
        if (!node.Properties.HasFunctionAssignment(nameof(onCheck)))
        {
            var onChangeFunctionBody = new TsLineCollection
            {
                // u p d a t e   s o u r c e
                from property in node.Properties
                where property.Name == nameof(@checked)
                from line in GetUpdateStateLines(property.Value, "isChecked")
                select line,

                // e v e n t   h a n d l e r
                from property in node.Properties
                where property.Name == nameof(onCheck)
                let value = property.Value
                select IsAlphaNumeric(value) ? value + "(e, isChecked);" : value
            };

            node = onChangeFunctionBody.HasLine ? node.UpdateProp(nameof(onCheck), new()
            {
                "(e: any, isChecked: boolean) =>",
                "{", onChangeFunctionBody, "}"
            }) : node;
        }
        

        node = AddContextProp(node);

        var import = (nameof(BCheckBox), "b-check-box");
        
        return AnalyzeChildren(input with{Node = node}, AnalyzeReactNode).With(import);
    }

    protected override Element render()
    {
        var svgForIsCheckedFalse = new svg(ViewBox(0, 0, 24, 24), Fill(rgb(22, 160, 133)))
        {
            new path { d = "M19 5v14H5V5h14m0-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2z" }
        };

        var svgForIsCheckedTrue = new svg(ViewBox(0, 0, 24, 24), Fill(rgb(22, 160, 133)))
        {
            new path { d = "M19 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.11 0 2-.9 2-2V5c0-1.1-.89-2-2-2zm-9 14l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" }
        };

        return new FlexRowCentered(Gap(12), WidthFitContent)
        {
            new FlexRowCentered(Size(24))
            {
                @checked == "true" ? svgForIsCheckedTrue : svgForIsCheckedFalse
            },
            new div { label },

            Id(base.id),
            OnClick(onMouseClick)
        };
    }
}