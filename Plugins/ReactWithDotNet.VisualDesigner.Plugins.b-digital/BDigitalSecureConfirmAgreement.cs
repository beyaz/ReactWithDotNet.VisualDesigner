namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalSecureConfirmAgreement : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string messageInfo { get; set; }

    [JsTypeInfo(JsType.String)]
    public string description { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string isRefresh { get; set; }

    [JsTypeInfo(JsType.String)]
    public string approveText { get; set; }
    
    [JsTypeInfo(JsType.Array)]
    public string documents { get; set; }
    
        
    [JsTypeInfo(JsType.Number)]
    public string duration { get; set; }
    
    [JsTypeInfo(JsType.Function)]
    public string setAgreementDocument { get; set; }
    
    [Suggestions("NextType.None , NextType.Page , NextType.Save")]
    [JsTypeInfo(JsType.String)]
    public string actionType { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalSecureConfirmAgreement))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        input = ApplyTranslateOperationOnProps(input, nameof(approveText), nameof(description));
        

        var node = input.Node;


        var import = (nameof(BDigitalSecureConfirmAgreement), "b-digital-secure-confirm-agreement");

        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
    }

    protected override Element render()
    {
        return new FlexColumn(MarginBottom(24), MarginTop(8))
        {
            Id(id), OnClick(onMouseClick),

            new FlexColumn(Background(White), BorderRadius(10), Border(1, solid, "#E0E0E0"), Padding(32))
            {
                new BAlert
                {
                    severity = "info",
                    children=
                    {
                        "İşlemi tamamlayabilmeniz için belgeleri Kuveyt Türk Mobil'de Belgelerim ekranından onaylamanız gerekmektedir. Belgeleri Onayla butonu ile Kuveyt Türk Mobil'e yönlendirileceksiniz."
                    }
                }
            },
            SpaceY(24),

            new div(FontSize18, FontWeight600, Color(rgba(0, 0, 0, 0.87))) { "Belge Onayı" },
            SpaceY(8),
            new FlexColumn(Background(White), BorderRadius(10), Border(1, solid, "#E0E0E0"), Padding(24))
            {
                new FlexRow(Gap(8), AlignItemsCenter)
                {
                    new svg(ViewBox(0, 0, 24, 24), svg.Size(24), Fill(rgb(22, 160, 133)))
                    {
                        new path { d = "M0 0h24v24H0z", fill = none},
                        new path { d = "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z"}
                    },

                    new div
                    {
                        "Bağış Tahsilat Aydınlatma Metni"
                    },

                    new div(Opacity(0.5),Border(1,solid, rgb(22, 160, 133)), Color(rgb(22, 160, 133)), PaddingX(8), PaddingY(4), BorderRadius(10))
                    {
                        "Onayla"
                    }
                },
                SpaceY(16),
                new FlexRowCentered
                {
                    "İşlemi tamamlayabilmeniz için belgeleri Kuveyt Türk Mobil'de Belgelerim ekranından onaylamanız gerekmektedir. Belgeleri Onayla butonu ile Kuveyt Türk Mobil'e yönlendirileceksiniz."
                }
            }
        };

    }
}