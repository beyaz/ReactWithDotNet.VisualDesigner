namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class TransactionWizardPage : PluginComponentBase
{
    [JsTypeInfo(JsType.Boolean)]
    public string isWide { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(TransactionWizardPage))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var import = (nameof(TransactionWizardPage),"b-digital-framework");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }
    
    protected override Element render()
    {
        return new FlexColumn(WidthFull, Padding(16), Background("#fafafa"))
        {
            children =
            {
                children
            }
        } + Id(id) + OnClick(onMouseClick);
    }
}