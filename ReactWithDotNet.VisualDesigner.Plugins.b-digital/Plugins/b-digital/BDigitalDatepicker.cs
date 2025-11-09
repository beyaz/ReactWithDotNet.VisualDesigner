using System.Collections.Immutable;
using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BDigitalDatepicker", Package = "b-digital-datepicker")]
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
    public static ReactNode AnalyzeReactNode(ReactNode node, ComponentConfig componentConfig)
    {
        if (node.Tag != nameof(BDigitalDatepicker))
        {
            return node with
            {
                Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList()
            };
        }

        node = ApplyTranslateOperationOnProps(node, componentConfig, nameof(labelText));

        var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
        var isRequiredProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isRequired));
        var onDateChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onDateChange));
        if (valueProp is not null)
        {
            var properties = node.Properties;

            List<string> lines =
            [
                "(value: Date) =>",
                "{",
                $"  {valueProp.Value} = value;",
                GetUpdateStateLine(valueProp.Value)
            ];

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
                    Value = string.Join(Environment.NewLine, lines)
                };

                properties = properties.SetItem(properties.FindIndex(x => x.Name == onDateChangeProp.Name), onDateChangeProp);
            }
            else
            {
                properties = properties.Add(new()
                {
                    Name  = nameof(onDateChange),
                    Value = string.Join(Environment.NewLine, lines)
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

        return node;
    }

    protected override Element render()
    {
        var textContent = string.Empty;
        if (labelText.HasValue())
        {
            textContent = labelText;
        }

        if (value.HasValue())
        {
            textContent += " | " + value;
        }

        return new div(WidthFull, PaddingTop(16), PaddingBottom(8))
        {
            Id(id), OnClick(onMouseClick),

            new FlexRow(AlignItemsCenter, PaddingLeft(16), PaddingRight(12), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
            {
                new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif"))
                {
                    textContent
                },
                new DynamicMuiIcon { name = "CalendarMonthOutlined", fontSize = "medium" }
            }
        };
    }
}