using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BDigitalAccountView", Package = "b-digital-account-view")]
sealed class BDigitalAccountView : PluginComponentBase
{
    [JsTypeInfo(JsType.Array)]
    public string accounts { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onSelectedAccountIndexChange { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string selectedAccountIndex { get; set; }

    [JsTypeInfo(JsType.String)]
    public string title { get; set; }
    

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalAccountView))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var (node, componentConfig) = input;




        


        
        
        {
            var selectedAccountIndexProp = node.Properties.FirstOrDefault(x => x.Name == nameof(selectedAccountIndex));
            var onSelectedAccountIndexChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onSelectedAccountIndexChange));

            if (selectedAccountIndexProp is not null)
            {
                var properties = node.Properties;

                List<string> lines =
                [
                    "(selectedAccountIndex: number) =>",
                    "{",
                    $"  updateRequest(r => {{ r.{selectedAccountIndexProp.Value.RemoveFromStart("request.")} = selectedAccountIndex; }});"
                ];

                if (onSelectedAccountIndexChangeProp is not null)
                {
                    if (IsAlphaNumeric(onSelectedAccountIndexChangeProp.Value))
                    {
                        lines.Add(onSelectedAccountIndexChangeProp.Value + "(selectedAccountIndex);");
                    }
                    else
                    {
                        lines.Add(onSelectedAccountIndexChangeProp.Value);
                    }
                }

                lines.Add("}");

                if (onSelectedAccountIndexChangeProp is not null)
                {
                    onSelectedAccountIndexChangeProp = onSelectedAccountIndexChangeProp with
                    {
                        Value = string.Join(Environment.NewLine, lines)
                    };

                    properties = properties.SetItem(properties.FindIndex(x => x.Name == onSelectedAccountIndexChangeProp.Name), onSelectedAccountIndexChangeProp);
                }
                else
                {
                    properties = properties.Add(new()
                    {
                        Name  = nameof(onSelectedAccountIndexChange),
                        Value = string.Join(Environment.NewLine, lines)
                    });
                }

                node = node with { Properties = properties };
            }
        }

        return AnalyzeChildren(input with{Node = node}, AnalyzeReactNode);
    }

    protected override Element render()
    {
        var textContent = string.Empty;
        if (title.HasValue())
        {
            textContent = title;
        }

        if (accounts.HasValue())
        {
            textContent += " | " + accounts;
        }

        return new div
        {
            Id(id), OnClick(onMouseClick),
            new FlexRow(AlignItemsCenter, Padding(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(83), JustifyContentSpaceBetween)
            {
                new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400) { textContent },

                new FlexRow(AlignItemsCenter, TextAlignRight, Gap(8))
                {
                    new FlexColumn
                    {
                        new div(FontWeight700) { "73.148,00 TL" },
                        new div(Color("rgb(0 0 0 / 60%)")) { "Cari Hesap" }
                    },

                    new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24), Color(rgb(117, 117, 117)))
                    {
                        new path
                        {
                            d    = "M7 10l5 5 5-5z",
                            fill = "#757575"
                        }
                    }
                }
            }
        };
    }
}