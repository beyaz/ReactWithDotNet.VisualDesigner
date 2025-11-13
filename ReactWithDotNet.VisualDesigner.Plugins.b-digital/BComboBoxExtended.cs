using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BComboBoxExtended", Package = "b-combo-box-extended")]
[Import(Name = "TextValuePair", Package = "b-digital-internet-banking")]
sealed class BComboBoxExtended : PluginComponentBase
{
    [JsTypeInfo(JsType.Array)]
    public string dataSource { get; set; }
    
    [JsTypeInfo(JsType.String)]
    public string label { get; set; }
    
    [JsTypeInfo(JsType.String)]
    public string displayMemberPath { get; set; }
    
    [JsTypeInfo(JsType.String)]
    public string valueMemberPath { get; set; }
    
    [JsTypeInfo(JsType.String)]
    public string value { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onChange { get; set; }
    
    [JsTypeInfo(JsType.Boolean)]
    public string isRequired { get; set; }
   

    

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BComboBoxExtended))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var (node, componentConfig) = input;
        
        {
            var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
            var onChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onChange));
            
            var isRequiredProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isRequired));
            var labelProp = node.Properties.FirstOrDefault(x => x.Name == nameof(label));

            if (valueProp is not null)
            {
                var properties = node.Properties;


                TsLineCollection lines =
                [
                    "(event: React.ChangeEvent<{}>, value: any, selectedObject?: any) =>",
                    "{",
                    $"  {valueProp.Value} = value;",
                    GetUpdateStateLine(valueProp.Value)
                ];

                if (onChangeProp is not null)
                {
                    if (IsAlphaNumeric(onChangeProp.Value))
                    {
                        lines.Add(onChangeProp.Value + "(event, value, selectedObject);");
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
                        Value = lines.ToTsCode()
                    };

                    properties = properties.SetItem(properties.FindIndex(x => x.Name == onChangeProp.Name), onChangeProp);
                }
                else
                {
                    properties = properties.Add(new()
                    {
                        Name  = nameof(onChange),
                        Value = lines.ToTsCode()
                    });
                }
                
                node = node with { Properties = properties };
            }
            
            if (isRequiredProp is not null && labelProp is not null)
            {
               
                node = node with
                {
                    Properties = node.Properties.Remove(isRequiredProp).Remove(labelProp).Add(new()
                    {
                        Name  = "inputProps",
                        Value = $$"""
                                  {
                                      floatingLabelText: {{labelProp.Value}},
                                      valueConstraint: { required: {{Plugin.ConvertDotNetPathToJsPath(isRequiredProp.Value)}} }
                                  }
                                  """
                    })
                };
            }
        }

        

        return AnalyzeChildren(input with{Node = node}, AnalyzeReactNode);
    }

    protected override Element render()
    {
        var textContent = string.Empty;
        if (label.HasValue())
        {
            textContent = label;
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

}