namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalGroupView : PluginComponentBase
{
    
    [JsTypeInfo(JsType.String)]
    public string title { get; set; }

    [JsTypeInfo(JsType.String)]
    public string subTitle { get; set; }
    
    
    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string applySideMargin { get; set; }
    
    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string applyVerticalMargin { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalGroupView))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var import = (nameof(BDigitalGroupView),"b-digital-group-view");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }
    
    
    protected override Element render()
    {
        var _applySideMargin = applySideMargin ?? "true";
        
        var _applyVerticalMargin = applyVerticalMargin ?? "true";

        var style = new Style();
        
        if (_applySideMargin == "true" && _applyVerticalMargin == "true")
        {
            style.Add(Padding(24));
            style.Add(MarginTop(8));
            style.Add(MarginBottom(24));
        }
        else if (_applySideMargin == "true")
        {
            style.Add(PaddingX(24));
            style.Add(MarginTop(8));
        }
        else if (_applyVerticalMargin == "true")
        {
            style.Add(PaddingY(24));
            style.Add(MarginTop(8));
        }
        else
        {
            style.Add(MarginTop(8));
        }

        return new Fragment
        {
            title.HasValue
                ? new BTypography
                {
                    variant  = "body0m",
                    children = { title },
                    
                    id           =id,
                    onMouseClick = onMouseClick
                }
                : null,

            subTitle.HasValue
                ? new BTypography
                {
                    variant  = "body2",
                    color = textSecondary,
                    children = { subTitle },
                    
                    id=id,
                    onMouseClick= onMouseClick
                }
                : null,

            new FlexColumn( Background(White), BorderRadius(10), Border(1, solid, "#E0E0E0"), style, Id(id), OnClick(onMouseClick))
            {
                children
            }
        };
        
    }

    [TryGetIconForElementTreeNode]
    public static Scope TryGetIconForElementTreeNode(Scope scope)
    {
        var model = Plugin.VisualElementModel[scope];
        
        if (model.Tag == nameof(BDigitalGroupView))
        {
            return Scope.Create(new()
            {
                { Plugin.IconForElementTreeNode, new IconPanel() }
            });
        }

        return Scope.Empty;
    }
}