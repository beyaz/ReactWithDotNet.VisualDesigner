namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BInputNumericExtended : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string errorText { get; set; }
    
    [JsTypeInfo(JsType.String)]
    public string floatingLabelText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string helperText { get; set; }
    
    [JsTypeInfo(JsType.Boolean)]
    public string isRequired { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string maxLength { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onChange { get; set; }

    [JsTypeInfo(JsType.String)]
    public string value { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string disabled { get; set; }
    
    [JsTypeInfo(JsType.String)]
    public string hintText { get; set; }

    [Suggestions("left , center , right")]
    [JsTypeInfo(JsType.String)]
    public string inputAlign { get; set; }

    [Suggestions("F , M, D")]
    [JsTypeInfo(JsType.String)]
    public string format { get; set; }
    
    [JsTypeInfo(JsType.Number)]
    public string minValue { get; set; }
    
    [JsTypeInfo(JsType.Number)]
    public string maxValue { get; set; }
    
   
    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BInputNumericExtended))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        input = ApplyTranslateOperationOnProps(input, nameof(floatingLabelText), nameof(errorText),nameof(helperText), nameof(hintText));
        
        var node = input.Node;



 

        if (!node.Properties.HasFunctionAssignment(nameof(onChange)))
        {
            var onChangeFunctionBody = new TsLineCollection
            {
                // u p d a t e   s o u r c e
                from property in node.Properties
                where property.Name == nameof(value)
                from line in GetUpdateStateLines(property.Value, "value")
                select line,

                // e v e n t   h a n d l e r
                from property in node.Properties
                where property.Name == nameof(onChange)
                let value = property.Value
                select IsAlphaNumeric(value) ? value + "(e, value);" : value
            };

            node = onChangeFunctionBody.HasLine ? node.UpdateProp(nameof(onChange), new()
            {
                "(e: any, value: any) =>",
                "{", onChangeFunctionBody, "}"
            }) : node;
        }
        
      
        var isRequiredProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isRequired));
        if (isRequiredProp is not null)
        {
            var newValue = new
            {
                required = Plugin.ConvertDotNetPathToJsPath(isRequiredProp.Value)
            };
            
            node = node with
            {
                Properties = node.Properties.Remove(isRequiredProp).Add(new()
                {
                    Name  = "valueConstraint",
                    Value = $"{{ required: {newValue.required} }}"
                })
            };
        }

        node = AddContextProp(node);
        
        return Result.From((node, new TsImportCollection
        {
            {nameof(BInputNumericExtended),"b-input-numeric-extended"}
        }));
    }

    protected override Element render()
    {
        return new div(PaddingTop(16), PaddingBottom(8))
        {
            Id(id), OnClick(onMouseClick),
            
            new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58))
            {
                // L a b e l   o n   t o p - l e f t   b o r d e r 
                PositionRelative,
                new label
                {
                    // c o n t e n t
                    floatingLabelText,
                    
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
                
                inputAlign switch
                {
                    "left"=> JustifyContentFlexStart,
                    
                    "right"=> JustifyContentFlexEnd,
                    
                    "center"=> JustifyContentCenter,
                    
                    _=> JustifyContentFlexEnd
                }
            },
            new FlexRow(JustifyContentSpaceBetween, FontSize12, PaddingLeftRight(14), Color(rgb(158, 158, 158)), LineHeight15)
            {
                new div { helperText },
                new div { maxLength }
            }
        };
    }
}