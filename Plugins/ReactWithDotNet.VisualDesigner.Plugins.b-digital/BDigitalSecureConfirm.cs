namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalSecureConfirm : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string smsPassword { get; set; }

    [JsTypeInfo(JsType.String)]
    public string messageInfo { get; set; }
    
    [JsTypeInfo(JsType.Boolean)]
    public string isPreDataLoaded { get; set; }
    
     

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalSecureConfirm))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var (node, componentConfig) = input;




        


        {
            var smsPasswordProp = node.Properties.FirstOrDefault(x => x.Name == nameof(smsPassword));

            if (smsPasswordProp is not null)
            {
                var properties = node.Properties;
                
                var lines = new TsLineCollection
                {
                    "(value: string) =>",
                    "{",
                    GetUpdateStateLines(smsPasswordProp.Value, "value"),
                    
                    "}"
                };
                
                

                properties = properties.Remove(smsPasswordProp).Add(new()
                {
                    Name  = "handleSmsPasswordSend",
                    Value = lines.ToTsCode()
                });

                node = node with { Properties = properties };
            }
        }

        var import = (nameof(BDigitalSecureConfirm), "b-digital-secure-confirm");
   
        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
    }

    protected override Element render()
    {
        return new div(Id(id), OnClick(onMouseClick), DisplayFlex, FlexDirectionColumn, MarginBottom(24), MarginTop(8), UserSelect(none))
        {
            new div(FontSize18, FontWeight600, LineHeight32, Color("rgba(0, 0, 0, 0.87)"))
            {
                "Mobil Onay"
            },
            new div(Id("0,1"), DisplayFlex, FlexDirectionRow, Gap(16), Background("repeating-linear-gradient(45deg, rgb(253, 224, 71) 0px, rgb(253, 224, 71) 1px, transparent 0px, transparent 50%) 0% 0% / 5px 5px, none 0% 0% repeat scroll padding-box border-box rgb(255, 255, 255)"), BorderRadius(10), Border("1px solid rgb(224, 224, 224)"), Padding(24), Outline("rgb(69, 151, 247) dashed 1px"))
            {
                new div(DisplayFlex, FlexDirectionColumn, WidthFull, Outline("rgb(69, 151, 247) dashed 1px"), Background("repeating-linear-gradient(-45deg, rgb(71, 253, 245) 0px, rgb(71, 253, 245) 1px, transparent 0px, transparent 50%) 0% 0% / 5px 5px, none 0% 0% repeat scroll padding-box border-box rgba(0, 0, 0, 0)"))
                {
                    new div(FontWeight500, Color(rgb(2, 136, 209)))
                    {
                        "Mobil Onay Bekleniyor..."
                    },
                    new div(FontWeight400)
                    {
                        "Ödeme işleminizi gerçekleştirmek için cihazınıza gelen Mobil Onay bildirimine belirtilen süre içerisinde onay vermeniz gerekmektedir."
                    }
                },
                new div(DisplayFlex, FlexDirectionRow, JustifyContentCenter, AlignItemsCenter, Width(100), Height(100), Outline("rgb(69, 151, 247) dashed 1px"), Background("repeating-linear-gradient(-45deg, rgb(71, 253, 245) 0px, rgb(71, 253, 245) 1px, transparent 0px, transparent 50%) 0% 0% / 5px 5px, none 0% 0% repeat scroll padding-box border-box rgba(0, 0, 0, 0)"))
                {
                    new div(DisplayFlex, FlexDirectionRow, JustifyContentCenter, AlignItemsCenter, FontSize24, Color(rgb(2, 136, 209)), Width(64), Height(64), Border("4px solid rgb(2, 136, 209)"), BorderRadius(100))
                    {
                        "29"
                    }
                }
            }
        };
    }
}