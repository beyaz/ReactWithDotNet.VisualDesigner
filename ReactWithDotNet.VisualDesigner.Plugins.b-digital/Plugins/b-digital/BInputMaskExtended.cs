using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BInputMaskExtended", Package = "b-input-mask-extended")]
sealed class BInputMaskExtended : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string floatingLabelText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string helperText { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string isAutoComplete { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string isReadonly { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string isRequired { get; set; }

    [JsTypeInfo(JsType.Array)]
    public string mask { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string maxLength { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onChange { get; set; }

    [JsTypeInfo(JsType.String)]
    public string value { get; set; }
            
    [JsTypeInfo(JsType.Boolean)]
    public string disabled { get; set; }
            

    [NodeAnalyzer]
    public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
    {
        if (node.Tag == nameof(BInputMaskExtended))
        {
            var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
            var onChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onChange));
            var isRequiredProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isRequired));
            var isAutoCompleteProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isAutoComplete));

            if (valueProp is not null)
            {
                var properties = node.Properties;

                List<string> lines =
                [
                    "(e: any, value: any) =>",
                    "{",
                    $"  updateRequest(r => {{ r.{valueProp.Value.RemoveFromStart("request.")} = value; }});"
                ];

                if (onChangeProp is not null)
                {
                    if (IsAlphaNumeric(onChangeProp.Value))
                    {
                        lines.Add(onChangeProp.Value + "(e, value);");
                    }
                    else
                    {
                        lines.Add(onChangeProp.Value);
                    }
                }

                lines.Add("}");

                if (onChangeProp is not null)
                {
                    onChangeProp = onChangeProp with
                    {
                        Value = string.Join(Environment.NewLine, lines)
                    };

                    properties = properties.SetItem(properties.FindIndex(x => x.Name == onChangeProp.Name), onChangeProp);
                }
                else
                {
                    properties = properties.Add(new()
                    {
                        Name  = "onChange",
                        Value = string.Join(Environment.NewLine, lines)
                    });
                }

                node = node with { Properties = properties };
            }

            if (isRequiredProp is not null && isAutoCompleteProp is not null)
            {
                var autoCompleteFinalValue = string.Empty;
                {
                    if ("true".Equals(isAutoCompleteProp.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        autoCompleteFinalValue = "'on'";
                    }
                    else if ("false".Equals(isAutoCompleteProp.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        autoCompleteFinalValue = "'off'";
                    }
                    else
                    {
                        autoCompleteFinalValue = $"{Plugin.ConvertDotNetPathToJsPath(isAutoCompleteProp.Value)} ? \"on\" : \"off\" }}";
                    }
                }

                node = node with
                {
                    Properties = node.Properties.Remove(isRequiredProp).Remove(isAutoCompleteProp).Add(new()
                    {
                        Name  = "valueConstraint",
                        Value = $"{{ required: {Plugin.ConvertDotNetPathToJsPath(isRequiredProp.Value)}, autoComplete: {autoCompleteFinalValue} }}"
                    })
                };
            }

            node = AddContextProp(node);
        }

        return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
    }

    protected override Element render()
    {
        var textContent = string.Empty;
        if (floatingLabelText.HasValue())
        {
            textContent = floatingLabelText;
        }

        if (value.HasValue())
        {
            textContent += " | " + value;
        }

        return new div(PaddingTop(16), PaddingBottom(8))
        {
            new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
            {
                new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif")) { textContent },

                Id(id), OnClick(onMouseClick)
            },
            new FlexRow(JustifyContentSpaceBetween, FontSize12, PaddingLeftRight(14), Color(rgb(158, 158, 158)), LineHeight15)
            {
                new div { helperText },
                new div { maxLength }
            }
        };
    }
}