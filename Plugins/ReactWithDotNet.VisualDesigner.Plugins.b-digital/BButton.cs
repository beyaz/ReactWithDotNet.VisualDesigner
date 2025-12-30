using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BButton : PluginComponentBase
{
    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string allowLabelCase { get; set; }

  

    [Suggestions("default , primary , secondary")]
    [JsTypeInfo(JsType.String)]
    public string colorType { get; set; }

    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string fullWidth { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onClick { get; set; }

    [JsTypeInfo(JsType.String)]
    public string text { get; set; }

    [Suggestions("contained , text , fab , icon")]
    [JsTypeInfo(JsType.String)]
    public string type { get; set; }

    [Suggestions("center , left , right")]
    [JsTypeInfo(JsType.String)]
    public string textPosition { get; set; }


         
    [Suggestions("small , medium , large")]
    [JsTypeInfo(JsType.String)]
    public string buttonSize { get; set; }

   

    internal Style style { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BButton))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        var node = input.Node;

        node = ApplyTranslateOperationOnProps(node, input.ComponentConfig, nameof(text));

        var import = (nameof(BButton), "b-button");

        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
    }

    protected override Element render()
    {
        Style defaultStyle = [BorderRadius(10), TextTransform(none)];

        if (colorType == "primary")
        {
            defaultStyle =
            [
                DisplayFlexRowCentered,
                TextTransform(none),
                FontWeightBold,
                BackgroundColor("#16A085"),
                Color("white"),
                BorderRadius(10)
            ];
        }

        if (type == "contained" || colorType == "default" ||
            (type is null && colorType is null))
        {
            defaultStyle =
            [
                DisplayFlexRowCentered,
                TextTransform(none),
                FontWeightBold,
                BackgroundColor("#d5d5d5"),
                Color("black"),
                BorderRadius(10)
            ];
        }

        if (colorType == "primary" && type == "text")
        {
            defaultStyle =
            [
                DisplayFlexRowCentered,
                TextTransform(none),
                FontWeightBold,
                Color("#16A085"),
                BorderRadius(10),
                LetterSpacing(0.4)
            ];
        }

        return new Button
        {
            fullWidth = fullWidth == "true",

            style = { defaultStyle, style },

            variant = type,
            
            color = colorType,

            id = id,

            onClick = onMouseClick,

            children =
            {
                new div { text }, children
            }
        };
    }
}