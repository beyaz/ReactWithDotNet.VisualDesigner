namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalMoneyInput : PluginComponentBase
{
    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string currencyVisible { get; set; }

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

        var node = input.Node;

        node = ApplyTranslateOperationOnProps(node, input.ComponentConfig, nameof(label));

        {
            var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
            var handleMoneyInputChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(handleMoneyInputChange));
            if (valueProp is not null)
            {
                var properties = node.Properties;

                var lines = new TsLineCollection
                {
                    "(value: number) =>",
                    "{",
                    GetUpdateStateLines(valueProp.Value, "value")

                };

                if (handleMoneyInputChangeProp is not null)
                {
                    if (IsAlphaNumeric(handleMoneyInputChangeProp.Value))
                    {
                        lines.Add(handleMoneyInputChangeProp.Value + "(value);");
                    }
                    else
                    {
                        lines.Add(handleMoneyInputChangeProp.Value);
                    }
                }

                lines.Add("}");

                if (handleMoneyInputChangeProp is not null)
                {
                    handleMoneyInputChangeProp = handleMoneyInputChangeProp with
                    {
                        Value = lines.ToTsCode()
                    };

                    properties = properties.SetItem(properties.FindIndex(x => x.Name == handleMoneyInputChangeProp.Name), handleMoneyInputChangeProp);
                }
                else
                {
                    properties = properties.Add(new()
                    {
                        Name = nameof(handleMoneyInputChange),
                        Value = lines.ToTsCode()
                    });
                }

                node = node with { Properties = properties };
            }
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
                //new div{ helperText},
                //new div{ maxLength }
            },

            Id(id), OnClick(onMouseClick)
        };
    }
}