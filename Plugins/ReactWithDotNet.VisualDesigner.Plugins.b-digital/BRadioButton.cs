namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BRadioButton : PluginComponentBase
{
    [JsTypeInfo(JsType.Boolean)]
    public string @checked { get; set; }

    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string defaultChecked { get; set; }

    [JsTypeInfo(JsType.String)]
    public string label { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onChange { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BRadioButton))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        input = ApplyTranslateOperationOnProps(input, nameof(label));
        

        var node = input.Node;


        return Result.From((node, new TsImportCollection
        {
            { nameof(BRadioButton), "b-radio-button" }
        }));
    }

    protected override Element render()
    {
        return new FlexRowCentered(Gap(12), WidthFitContent)
        {
            Id(id),
            OnClick(onMouseClick),
            new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24), Fill(rgb(22, 160, 133)))
            {
                new path { d = "M12 7c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5zm0-5C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8z" }
            },

            new label
            {
                FontSize16, FontWeight400, LineHeight(1.5), FontFamily("Roboto, sans-serif"),
                label
            }
        };
    }
}