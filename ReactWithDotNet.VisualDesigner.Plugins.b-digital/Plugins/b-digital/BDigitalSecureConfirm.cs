using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = nameof(BDigitalSecureConfirm), Package = "b-digital-secure-confirm")]
sealed class BDigitalSecureConfirm : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string smsPassword { get; set; }

    [JsTypeInfo(JsType.String)]
    public string messageInfo { get; set; }
            
    [NodeAnalyzer]
    public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
    {
        if (node.Tag == nameof(BDigitalSecureConfirm))
        {
            var smsPasswordProp = node.Properties.FirstOrDefault(x => x.Name == nameof(smsPassword));

            if (smsPasswordProp is not null)
            {
                var properties = node.Properties;

                List<string> lines =
                [
                    "(value: string) =>",
                    "{",
                    $"  {smsPasswordProp.Value} = value;",
                    GetUpdateStateLine(smsPasswordProp.Value),
                    "}"
                ];

                properties = properties.Remove(smsPasswordProp).Add(new()
                {
                    Name  = "handleSmsPasswordSend",
                    Value = string.Join(Environment.NewLine, lines)
                });

                node = node with { Properties = properties };
            }
        }

        return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
    }

    protected override Element render()
    {
        return new FlexColumn(MarginBottom(24),MarginTop(8))
        {
            new div(FontSize18, FontWeight600, LineHeight32, Color(rgba(0, 0, 0, 0.87))) { "Mobil Onay" },
                    
            new FlexRow(Gap(16) ,Background(White), BorderRadius(10), Border(1, solid, "#E0E0E0"), Padding(24), Id(id), OnClick(onMouseClick))
            {
                new FlexColumn(WidthFull)
                {
                    new div(FontWeight500)
                    {
                        "Mobil Onay Bekleniyor...",
                        Color(rgba(2, 136, 209, 1))
                    },
                        
                    new div(FontWeight400)
                    {
                        "Ödeme işleminizi gerçekleştirmek için cihazınıza gelen Mobil Onay bildirimine belirtilen süre içerisinde onay vermeniz gerekmektedir."
                    }
                },
                        
                new FlexRowCentered(Size(100))
                {
                    new FlexRowCentered
                    {
                        "29",
                        FontSize24,
                        Color(rgba(2, 136, 209, 1)),
                        Size(64),
                        Border(4,solid, rgba(2, 136, 209, 1)),
                        BorderRadius(100)
                    }
                }
            }
        };
    }
}