using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BDigitalMoneyInput", Package = "b-digital-money-input")]
sealed class BDigitalMoneyInput : PluginComponentBase
{
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
    public static ReactNode AnalyzeReactNode(ReactNode node, ComponentConfig componentConfig)
    {
        if (node.Tag == nameof(BDigitalMoneyInput))
        {
            var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
            var handleMoneyInputChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(handleMoneyInputChange));
            if (valueProp is not null)
            {
                var properties = node.Properties;

                List<string> lines =
                [
                    "(value: number) =>",
                    "{",
                    $"  updateRequest(r => {{ r.{valueProp.Value.RemoveFromStart("request.")} = value; }});"
                ];

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
                        Value = string.Join(Environment.NewLine, lines)
                    };

                    properties = properties.SetItem(properties.FindIndex(x => x.Name == handleMoneyInputChangeProp.Name), handleMoneyInputChangeProp);
                }
                else
                {
                    properties = properties.Add(new()
                    {
                        Name  = nameof(handleMoneyInputChange),
                        Value = string.Join(Environment.NewLine, lines)
                    });
                }

                node = node with { Properties = properties };
            }
        }

        return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
    }

    protected override Element render()
    {
        var textContent = label ?? "Tutar";

        if (value.HasValue())
        {
            textContent += " | " + value;
        }

        return new div(WidthFull, PaddingTop(16), PaddingBottom(8))
        {
            new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
            {
                new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif"))
                {
                    textContent
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