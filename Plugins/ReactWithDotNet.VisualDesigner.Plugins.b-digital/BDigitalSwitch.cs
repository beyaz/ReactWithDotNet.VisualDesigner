using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalSwitch : PluginComponentBase
{
    [JsTypeInfo(JsType.Boolean)]
    public string @checked { get; set; }

    [Suggestions("true")]
    [JsTypeInfo(JsType.Boolean)]
    public string disabled { get; set; }

    [JsTypeInfo(JsType.String)]
    public new string id { get; set; }

    [JsTypeInfo(JsType.String)]
    public string label { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onChange { get; set; }
    
    [JsTypeInfo(JsType.String)]
    public string errorText { get; set; }
    
    [Suggestions("true")]
    [JsTypeInfo(JsType.Boolean)]
    public string errorTextVisible { get; set; }


    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalSwitch))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        input = ApplyTranslateOperationOnProps(input, nameof(label), nameof(errorText));
        
        
        var node = input.Node;

        node = Run(node, [
            Transforms.OnChange
        ]);


        var import = (nameof(BDigitalSwitch), "b-digital-switch");

        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
    }

    protected override Element render()
    {
        return new FlexColumn
        {
            new FlexRow(Gap(8), WidthFitContent)
            {
                new Switch{ disabled = true },
            
                new div { label },

                
            },
            
            new div { errorText, Color(Red600) },
            
            Id(base.id),
            OnClick(onMouseClick)
        };
    }
    
    
    static class Transforms
    {
        internal static ReactNode OnChange(ReactNode node)
        {
            if (!node.Properties.HasFunctionAssignment(nameof(onChange)))
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
                    where property.Name == nameof(onChange)
                    let value = property.Value
                    select IsAlphaNumeric(value) ? value + "(e, isChecked);" : value
                };

                node = onChangeFunctionBody.HasLine ? node.UpdateProp(nameof(onChange), new()
                {
                    "(e: any, isChecked: boolean) =>",
                    "{", onChangeFunctionBody, "}"
                }) : node;
            }

            return node;
        }
    }
}