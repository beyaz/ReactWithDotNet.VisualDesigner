namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalPhone : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string label { get; set; }

    [JsTypeInfo(JsType.String)]
    public string hintText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string phoneNumber { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string handlePhoneChange { get; set; }
    
    [Suggestions("true, ___any_text___")]
    [JsTypeInfo(JsType.String)]
    public string required { get; set; }


    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalPhone))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        input = ApplyTranslateOperationOnProps(input, nameof(label), nameof(hintText));
        
        
        var node = input.Node;

        node = Run(node, [
            Transforms.OnChange,
            Transform_inputProps
        ]);


        var import = (nameof(BDigitalPhone), "b-digital-phone");

        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
    }

    protected override Element render()
    {
        return new div(PaddingTop(16), PaddingBottom(8))
        {
            Id(id), OnClick(onMouseClick),

            new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
            {
                // L a b e l   o n   t o p - l e f t   b o r d e r 
                PositionRelative,
                new label
                {
                    // c o n t e n t
                    label,

                    // l a y o u t
                    PositionAbsolute,
                    Top(-6),
                    Left(16),
                    PaddingX(4),

                    // t h e m e
                    Color(rgba(0, 0, 0, 0.6)),
                    FontSize12,
                    FontWeight400,
                    LineHeight12,
                    LetterSpacing(0.15),
                    FontFamily("Roboto"),
                    Background(White)
                },

                new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif"))
                {
                    phoneNumber
                }
            },
            //new FlexRow(JustifyContentSpaceBetween, FontSize12, PaddingLeftRight(14), Color(rgb(158, 158, 158)), LineHeight15)
            //{
            //    new div { helperText },
            //    new div { maxLength }
            //}
        };
    }
    
    
    static class Transforms
    {
      

        internal static ReactNode OnChange(ReactNode node)
        {
            if (node.Properties.HasFunctionAssignment(nameof(handlePhoneChange)))
            {
                return node;
            }

            var onChangeFunctionBody = new TsLineCollection
            {
                // u p d a t e   s o u r c e
                from property in node.Properties
                where property.Name == nameof(phoneNumber)
                from line in GetUpdateStateLines(property.Value, "value")
                select line,

                // e v e n t   h a n d l e r
                from property in node.Properties
                where property.Name == nameof(handlePhoneChange)
                let value = property.Value
                select IsAlphaNumeric(value) ? value + "(value, formattedValue, areaCode);" : value
            };

            return !onChangeFunctionBody.HasLine ? node: node.UpdateProp(nameof(handlePhoneChange), new()
            {
                "(value: string, formattedValue: string, areaCode: string) =>",
                "{", onChangeFunctionBody, "}"
            });
        }
    }
}