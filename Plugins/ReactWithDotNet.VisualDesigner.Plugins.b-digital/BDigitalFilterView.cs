using System.Collections.Immutable;
using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = nameof(BDigitalFilterView), Package = "b-digital-filter-view")]
sealed class  BDigitalFilterView: PluginComponentBase
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
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalFilterView))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var (node, componentConfig) = input;




        


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
                    Value = new TsLineCollection
                    {
                        "(value: Date) =>", "{",
                        
                        $"  {beginDateProp.Value} = value;",
                        
                        GetUpdateStateLine(beginDateProp.Value),
                        
                        "}"
                    }.ToTsCode()
                });

                properties = properties.Add(new()
                {
                    Name  = "endDateDefault",
                    Value = endDateProp.Value
                });
                properties = properties.Add(new()
                {
                    Name  = "setEndDate",
                    Value = new TsLineCollection
                    {
                        "(value: Date) =>", "{",
                        
                        $"  {endDateProp.Value} = value;",
                        
                        GetUpdateStateLine(endDateProp.Value),
                        
                        "}"
                    }.ToTsCode()
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

        return AnalyzeChildren(input with{Node = node}, AnalyzeReactNode);
    }

    protected override Element render()
    {
        return new FlexColumn(PaddingTop(16))
        {
            Id(id), OnClick(onMouseClick),

            new FlexColumn(PaddingBottom(8), FontSize18)
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
                    new BSymbol { symbol = "Calendar_Month", weight = "400", color = rgb(22, 160, 133)}
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
                    
                    new BSymbol { symbol = "Calendar_Month", weight = "400", color = rgb(22, 160, 133)}
                    
                }
            },

            children,

            new BButton
            {
                text      = "Sonuçları Göster",
                type      ="raised",
                colorType ="primary",
                fullWidth = "true",
                style=new Style(MarginTop(8))
            }
        };
    }
}