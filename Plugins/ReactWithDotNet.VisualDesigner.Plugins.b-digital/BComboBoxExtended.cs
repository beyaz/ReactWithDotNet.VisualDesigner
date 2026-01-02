namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
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

    [Suggestions("true")]
    [JsTypeInfo(JsType.Boolean)]
    public string isRequired { get; set; }
    
    [Suggestions("true")]
    [JsTypeInfo(JsType.Boolean)]
    public string multiple { get; set; }

    [Suggestions("true")]
    [JsTypeInfo(JsType.Boolean)]
    public string disabled { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BComboBoxExtended))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        var node = input.Node;

        node = ApplyTranslateOperationOnProps(node, input.ComponentConfig, nameof(label));

        var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
        var onChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onChange));
        var isOnChangePropFunctionAssignment = onChangeProp is not null && onChangeProp.Value.Contains(" => ");

        var isRequiredProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isRequired));
        var labelProp = node.Properties.FirstOrDefault(x => x.Name == nameof(label));
        
        var isMultiple = node.Properties.FirstOrDefault(x => x.Name == nameof(multiple))?.Value == "true";

        if (valueProp is not null && !isOnChangePropFunctionAssignment)
        {
            var properties = node.Properties;


            var lines = new TsLineCollection();
           
            if (isMultiple)
            {
                lines.Add("(event: React.ChangeEvent<{}>, values: any[], selectedObjects?: any[]) =>");
                lines.Add("{");
                lines.Add(GetUpdateStateLines(valueProp.Value, "values"));
            }
            else
            {
                lines.Add("(event: React.ChangeEvent<{}>, value: any, selectedObject?: any) =>");
                lines.Add("{");
                lines.Add(GetUpdateStateLines(valueProp.Value, "value"));
            }

            if (onChangeProp is not null)
            {
                if (IsAlphaNumeric(onChangeProp.Value))
                {
                    if (isMultiple)
                    {
                        lines.Add(onChangeProp.Value + "(event, values, selectedObjects);");
                    }
                    else
                    {
                        lines.Add(onChangeProp.Value + "(event, value, selectedObject);");
                    }
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
                    Name = nameof(onChange),
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
                    Name = "inputProps",
                    Value = $$"""
                                  {
                                      floatingLabelText: {{labelProp.Value}},
                                      valueConstraint: { required: {{Plugin.ConvertDotNetPathToJsPath(isRequiredProp.Value)}} }
                                  }
                                  """
                })
            };
        }




        var import = (nameof(BComboBoxExtended), "b-combo-box-extended");

        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
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
                    label,
                    
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