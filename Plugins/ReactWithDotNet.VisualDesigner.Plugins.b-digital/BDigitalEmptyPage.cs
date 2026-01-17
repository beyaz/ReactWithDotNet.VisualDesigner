namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed partial class BDigitalEmptyPage : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string description { get; set; }

    [Suggestions("NoAccount , WorkTime , NothingFoundInSearch , NoPackage , NoCurrentAccount , CuriositySearch , Dashboard")]
    [JsTypeInfo(JsType.String)]
    public string infoImageSource { get; set; }

    
    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalEmptyPage))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }

        input = ApplyTranslateOperationOnProps(input, nameof(description));
        
        var import = (nameof(BDigitalEmptyPage),"b-digital-empty-page");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }
    
    
    protected override Element render()
    {
        return new div
        {
            DisplayFlex, JustifyItems("center"), AlignItemsCenter, FlexDirectionColumn, Height("100%"),

            Id(id), OnClick(onMouseClick),

            new img
            {
                loading = "lazy",
                src     = FirstOrDefaultOf(from x in Images where x.Name == infoImageSource select x.Src) ?? Images[0].Src
            },

            SpaceY(24),
            new BTypography
            {
                variant = "body2",
                color   = "rgba(0, 0, 0, 0.6)",
                children = { description }
            },

            FontWeight400,
            FontSize14,
            LineHeight(1.43),
            TextAlignCenter,

            SpaceY(24)
        };
    }
}