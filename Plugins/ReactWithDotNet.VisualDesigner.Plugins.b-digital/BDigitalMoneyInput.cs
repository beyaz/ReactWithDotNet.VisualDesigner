namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalMoneyInput : PluginComponentBase
{
    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string currencyVisible { get; set; }

    [JsTypeInfo(JsType.String)]
    public string errorText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string fec { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string handleMoneyInputChange { get; set; }

    [JsTypeInfo(JsType.String)]
    public string label { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string value { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalMoneyInput))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        input = ApplyTranslateOperationOnProps(input, nameof(label), nameof(errorText));

        var node = input.Node;

        if (!node.Properties.HasFunctionAssignment(nameof(handleMoneyInputChange)))
        {
            var onChangeFunctionBody = new TsLineCollection
            {
                // u p d a t e   s o u r c e
                from property in node.Properties
                where property.Name == nameof(value)
                from line in GetUpdateStateLines(property.Value, "value")
                select line,

                // e v e n t   h a n d l e r
                from property in node.Properties
                where property.Name == nameof(handleMoneyInputChange)
                let value = property.Value
                select IsAlphaNumeric(value) ? value + "(value);" : value
            };

            node = onChangeFunctionBody.HasLine
                ? node.UpdateProp(nameof(handleMoneyInputChange), new()
                {
                    "(value: number) =>",
                    "{", onChangeFunctionBody, "}"
                })
                : node;
        }

        var errorTextProp = node.Properties.FirstOrDefault(x => x.Name == nameof(errorText));
        if (errorTextProp is not null)
        {
            node = node with
            {
                Properties = node.Properties.Remove(errorTextProp).Add(new()
                {
                    Name  = "inputProps",
                    Value = $"{{ errorText: {errorTextProp.Value} }}"
                })
            };
        }

        var import = (nameof(BDigitalMoneyInput), "b-digital-money-input");

        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
    }

    protected override Element render()
    {
        return new div(WidthFull, PaddingTop(16), PaddingBottom(8))
        {
            new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
            {
                // L a b e l   o n   t o p - l e f t   b o r d e r 
                PositionRelative,
                new label
                {
                    // c o n t e n t
                    label ?? "Tutar",

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
                    value
                },

                new div { fec ?? "TL" }
            },
            new FlexRow(JustifyContentSpaceBetween, FontSize12, PaddingLeftRight(14), Color(rgb(158, 158, 158)), LineHeight15)
            {
                Color(rgb(211, 47, 47)),

                new div { errorText },
                //new div{ maxLength }
            },

            Id(id), OnClick(onMouseClick)
        };
    }
}