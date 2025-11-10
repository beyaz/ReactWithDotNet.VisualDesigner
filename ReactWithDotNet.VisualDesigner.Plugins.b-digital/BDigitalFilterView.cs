using System.Collections.Immutable;
using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BDigitalFilterView", Package = "b-digital-filter-view")]
sealed class BDigitalFilterView : PluginComponentBase
{
    [JsTypeInfo(JsType.Date)]
    public string beginDate { get; set; }

    [JsTypeInfo(JsType.String)]
    public string beginDateLabel { get; set; }

    [JsTypeInfo(JsType.Date)]
    public string endDate { get; set; }

    [JsTypeInfo(JsType.String)]
    public string endDateLabel { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onFilterDetailClear { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string setFilter { get; set; }

    [NodeAnalyzer]
    public static ReactNode AnalyzeReactNode(ReactNode node, ComponentConfig componentConfig)
    {
        if (node.Tag == nameof(BDigitalFilterView))
        {
            var beginDateProp = node.Properties.FirstOrDefault(x => x.Name == nameof(beginDate));
            var endDateProp = node.Properties.FirstOrDefault(x => x.Name == nameof(endDate));
            var setFilterProp = node.Properties.FirstOrDefault(x => x.Name == nameof(setFilter));

            if (beginDateProp is not null && endDateProp is not null && setFilterProp is not null)
            {
                var properties = node.Properties;

                properties = properties.Add(new()
                {
                    Name  = "beginDateDefault",
                    Value = beginDateProp.Value
                });

                properties = properties.Add(new()
                {
                    Name  = "setBeginDate",
                    Value = $"(value: Date) => {{ updateRequest(r => r.{beginDateProp.Value.RemoveFromStart("request.")} = value) }}"
                });

                properties = properties.Add(new()
                {
                    Name  = "endDateDefault",
                    Value = endDateProp.Value
                });
                properties = properties.Add(new()
                {
                    Name  = "setEndDate",
                    Value = $"(value: Date) => {{ updateRequest(r => r.{beginDateProp.Value.RemoveFromStart("request.")} = value) }}"
                });

                // setFilter
                {
                    List<string> lines =
                    [
                        "() =>",
                        "{"
                    ];

                    if (IsAlphaNumeric(setFilterProp.Value))
                    {
                        lines.Add(setFilterProp.Value + "();");
                    }
                    else
                    {
                        lines.Add(setFilterProp.Value);
                    }

                    lines.Add("}");

                    setFilterProp = setFilterProp with
                    {
                        Value = string.Join(Environment.NewLine, lines)
                    };

                    properties = properties.SetItem(properties.FindIndex(x => x.Name == setFilterProp.Name), setFilterProp);
                }

                properties = properties.Remove(beginDateProp).Remove(endDateProp);

                node = node with { Properties = properties };
            }
        }

        return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
    }

    protected override Element render()
    {
        return new FlexColumn(PaddingTop(16))
        {
            Id(id), OnClick(onMouseClick),

            new FlexColumn(PaddingY(16), FontSize18)
            {
                "Filtrele"
            },
            new div(WidthFull, PaddingTop(16), PaddingBottom(8))
            {
                new FlexRow(AlignItemsCenter, PaddingLeft(16), PaddingRight(12), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                {
                    new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif"))
                    {
                        beginDateLabel + " | " + beginDate
                    },
                    new DynamicMuiIcon { name = "CalendarMonthOutlined", fontSize = "medium" }
                }
            },

            new div(WidthFull, PaddingTop(16), PaddingBottom(8))
            {
                new FlexRow(AlignItemsCenter, PaddingLeft(16), PaddingRight(12), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                {
                    new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif"))
                    {
                        endDateLabel + " | " + endDate
                    },
                    new DynamicMuiIcon { name = "CalendarMonthOutlined", fontSize = "medium" }
                }
            },

            children,

            new BButton
            {
                text = "Sonuçları Göster"
            }
        };
    }
}