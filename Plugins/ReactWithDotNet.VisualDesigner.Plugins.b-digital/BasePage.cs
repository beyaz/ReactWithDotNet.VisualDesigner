namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BasePage : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string pageTitle { get; set; }
    
    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string isWide { get; set; }
    
    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string needMobileApproval { get; set; }
    
    [JsTypeInfo(JsType.Function)]
    public string handleBackClick { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BasePage))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        input = ApplyTranslateOperationOnProps(input, nameof(pageTitle));
        
        var node = input.Node;


        var import = (nameof(BasePage),"b-digital-framework");
        
        return  AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
    }
    
    protected override Element render()
    {
        return new FlexColumn(FontFamily("Roboto"), WidthFull, Padding(16), Background("#fafafa"))
        {
            children =
            {
                new h6(FontWeight500, FontSize20, PaddingTop(32), PaddingBottom(56))
                {
                    pageTitle
                },
                children
            }
        } + Id(id) + OnClick(onMouseClick);
    }
}