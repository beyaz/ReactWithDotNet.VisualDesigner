namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
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

    [JsTypeInfo(JsType.Date)]
    public string beginMinDate { get; set; }
    
    [JsTypeInfo(JsType.Date)]
    public string beginMaxDate { get; set; }

    [JsTypeInfo(JsType.Date)]
    public string endMinDate { get; set; }
    
    [JsTypeInfo(JsType.Date)]
    public string endMaxDate { get; set; }

    
    
        

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalFilterView))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        var node = input.Node;


        node = ApplyTranslateOperationOnProps(node, input.ComponentConfig, nameof(beginDateLabel));
        node = ApplyTranslateOperationOnProps(node, input.ComponentConfig, nameof(endDateLabel));

        


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
                        
                       
                        GetUpdateStateLines(beginDateProp.Value, "value"),
                        
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
                        
                        
                        GetUpdateStateLines(endDateProp.Value, "value"),
                        
                        
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

        var import = (nameof(BDigitalFilterView), "b-digital-filter-view");
   
        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
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
            
            new BDigitalDatepicker
            {
                labelText = beginDateLabel ?? "Başlangıç Tarihi",
                value     = beginDate
            },
            
            new BDigitalDatepicker
            {
                labelText = endDateLabel ?? "Bitiş Tarihi",
                value     = endDate
            },
          
            children,

            new BButton
            {
                text      = "Sonuçları Göster",
                type      ="raised",
                colorType ="primary",
                fullWidth = "true",
                style=new Style(MarginTop(8), Background("#16A085"), Color("#fff"))
            }
        };
    }
}