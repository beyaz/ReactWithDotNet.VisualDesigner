namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalDatepicker : PluginComponentBase
{
    [JsTypeInfo(JsType.Boolean)]
    public string disabled { get; set; }

    [JsTypeInfo(JsType.String)]
    public string format { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string isRequired { get; set; }

    [JsTypeInfo(JsType.String)]
    public string labelText { get; set; }

    [JsTypeInfo(JsType.Date)]
    public string maxDate { get; set; }

    [JsTypeInfo(JsType.Date)]
    public string minDate { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onDateChange { get; set; }

    [JsTypeInfo(JsType.String)]
    public string placeholder { get; set; }

    [JsTypeInfo(JsType.Date)]
    public string value { get; set; }

    
    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalDatepicker))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var (node, componentConfig) = input;
        
        node = ApplyTranslateOperationOnProps(node, componentConfig, nameof(labelText));

        var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
        var isRequiredProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isRequired));
        var onDateChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onDateChange));
        if (valueProp is not null)
        {
            var properties = node.Properties;
            
            var lines = new TsLineCollection
            {
                "(value: Date) =>",
                "{",
                GetUpdateStateLines(valueProp.Value, "value")
            };
            

            if (onDateChangeProp is not null)
            {
                if (IsAlphaNumeric(onDateChangeProp.Value))
                {
                    lines.Add(onDateChangeProp.Value + "(value);");
                }
                else
                {
                    lines.Add(onDateChangeProp.Value);
                }
            }

            lines.Add("}");

            if (onDateChangeProp is not null)
            {
                onDateChangeProp = onDateChangeProp with
                {
                    Value = lines.ToTsCode()
                };

                properties = properties.SetItem(properties.FindIndex(x => x.Name == onDateChangeProp.Name), onDateChangeProp);
            }
            else
            {
                properties = properties.Add(new()
                {
                    Name  = nameof(onDateChange),
                    Value = lines.ToTsCode()
                });
            }

            node = node with { Properties = properties };
        }

        var placeholderProp = node.Properties.FirstOrDefault(x => x.Name == nameof(placeholder));
        if (placeholderProp is not null)
        {
            var placeholderFinalValue = string.Empty;
            {
                if (IsStringValue(placeholderProp.Value))
                {
                    placeholderFinalValue = placeholderProp.Value;
                }
                else
                {
                    placeholderFinalValue = $"{Plugin.ConvertDotNetPathToJsPath(placeholderProp.Value)}";
                }
            }

            node = node with
            {
                Properties = node.Properties.Remove(placeholderProp).Add(new()
                {
                    Name  = "inputProps",
                    Value = $"{{ placeholder: {placeholderFinalValue} }}"
                })
            };
        }

        if (isRequiredProp is not null)
        {
            node = node with
            {
                Properties = node.Properties.Remove(isRequiredProp).Add(new()
                {
                    Name  = "valueConstraint",
                    Value = $"{{ required: {Plugin.ConvertDotNetPathToJsPath(isRequiredProp.Value)} }}"
                })
            };
        }

        return Result.From((node, new TsImportCollection
        {
            { nameof(BDigitalDatepicker), "b-digital-datepicker" }
        }));
    }

    protected override Element render()
    {
        return new div(WidthFull, PaddingTop(16), PaddingBottom(8))
        {
            Id(id), OnClick(onMouseClick),

            new FlexRow(AlignItemsCenter, PaddingLeft(16), PaddingRight(12), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
            {
                // L a b e l   o n   t o p - l e f t   b o r d e r 
                PositionRelative,
                new label
                {
                    // c o n t e n t
                    labelText,
                    
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
                new BSymbol { symbol = "Calendar_Month", weight = "500", color = rgb(22, 160, 133)}
            }
        }; 
    }
}