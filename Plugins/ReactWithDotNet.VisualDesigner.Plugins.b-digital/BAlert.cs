using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;


namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BAlert : PluginComponentBase
{
    [Suggestions("success , info , warning , error")]
    [JsTypeInfo(JsType.String)]
    public string severity { get; set; }

    [Suggestions("standard , outlined , filled")]
    [JsTypeInfo(JsType.String)]
    public string variant { get; set; }


    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BAlert))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var import = (nameof(BAlert),"b-core-alert");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }

    protected override Element render()
    {
        return new div
        {
            Id(id), OnClick(onMouseClick),

            new Alert
            {
                severity = severity,
                
                variant  = variant,

                children = { children }
            }
        };
    }
}