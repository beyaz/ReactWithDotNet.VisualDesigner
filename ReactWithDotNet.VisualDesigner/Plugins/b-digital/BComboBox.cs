﻿using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BComboBox", Package = "b-combo-box")]
[Import(Name = "TextValuePair", Package = "b-digital-internet-banking")]
sealed class BComboBox : PluginComponentBase
{
    [JsTypeInfo(JsType.Array)]
    public string dataSource { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string hiddenClearButton { get; set; }

    [JsTypeInfo(JsType.String)]
    public string hintText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string labelText { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onSelect { get; set; }

    [JsTypeInfo(JsType.String)]
    public string value { get; set; }

    [NodeAnalyzer]
    public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
    {
        if (node.Tag == nameof(BComboBox))
        {
            var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
            var onSelectProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onSelect));

            if (valueProp is not null)
            {
                var properties = node.Properties;

                var isCollection = IsPropertyPathProvidedByCollection(componentConfig, valueProp.Value);

                TsLineCollection lines =
                [
                    "(selectedIndexes: [number], selectedItems: [TextValuePair], selectedValues: [string]) =>",
                    "{",
                    $"  {valueProp.Value} = selectedValues{(isCollection ? string.Empty : "[0]")};",
                    GetUpdateStateLine(valueProp.Value)
                ];

                if (onSelectProp is not null)
                {
                    if (IsAlphaNumeric(onSelectProp.Value))
                    {
                        lines.Add(onSelectProp.Value + "(selectedIndexes, selectedItems, selectedValues);");
                    }
                    else
                    {
                        lines.Add(onSelectProp.Value);
                    }
                }

                lines.Add("}");

                if (onSelectProp is not null)
                {
                    onSelectProp = onSelectProp with
                    {
                        Value = lines.ToTsCode()
                    };

                    properties = properties.SetItem(properties.FindIndex(x => x.Name == onSelectProp.Name), onSelectProp);
                }
                else
                {
                    properties = properties.Add(new()
                    {
                        Name  = nameof(onSelect),
                        Value = lines.ToTsCode()
                    });
                }

                if (!isCollection)
                {
                    properties = properties.SetItem(properties.IndexOf(valueProp), valueProp with
                    {
                        Value = $"[{valueProp.Value}]"
                    });
                }

                node = node with { Properties = properties };
            }
        }

        return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
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
                new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24), Color(rgb(117, 117, 117)))
                {
                    new path
                    {
                        d    = "M7 10l5 5 5-5z",
                        fill = "#757575"
                    }
                }
            },
            new FlexRow(JustifyContentSpaceBetween, FontSize12, PaddingLeftRight(14), Color(rgb(158, 158, 158)), LineHeight15)
            {
                //new div{ helperText},
                //new div{ maxLength }
            }
        };
    }

    static bool IsPropertyPathProvidedByCollection(IReadOnlyDictionary<string, string> componentConfig, string propertyPathWithVariableName)
    {
        foreach (var (variableName, dotNetAssemblyFilePath, dotnetTypeFullName) in Plugin.GetDotNetVariables(componentConfig))
        {
            if (!propertyPathWithVariableName.StartsWith(variableName + ".", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var propertyPath = propertyPathWithVariableName.RemoveFromStart(variableName + ".");

            return CecilHelper.IsPropertyPathProvidedByCollection(dotNetAssemblyFilePath, dotnetTypeFullName, propertyPath);
        }

        return false;
    }
}