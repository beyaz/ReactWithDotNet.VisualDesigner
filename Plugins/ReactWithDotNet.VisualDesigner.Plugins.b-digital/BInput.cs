namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BInput : PluginComponentBase
{
    [JsTypeInfo(JsType.Boolean)]
    public string disabled { get; set; }

    [JsTypeInfo(JsType.String)]
    public string errorText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string floatingLabelText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string helperText { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string isAutoComplete { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string isRequired { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string maxLength { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onChange { get; set; }

    [JsTypeInfo(JsType.String)]
    public string value { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BInput))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        var node = input.Node;

        node = ApplyTranslateOperationOnProps(node, input.ComponentConfig, nameof(floatingLabelText));

        var isOnChangePropFunctionAssignment = node.Properties.HasFunctionAssignment(nameof(onChange));
        if (!isOnChangePropFunctionAssignment)
        {
            var lines = new TsLineCollection
            {
                // v a l u e
                from property in node.Properties
                where property.Name == nameof(value)
                from line in GetUpdateStateLines(property.Value, "value")
                select line,

                // o n c h a n g e
                from property in node.Properties
                where property.Name == nameof(onChange)
                let value = property.Value
                select IsAlphaNumeric(value) ? value + "(e, value);" : value
            };

            node = lines.HasLine ? node.UpdateProp(nameof(onChange), new()
            {
                "(e: any, value: any) =>",
                "{", lines, "}"
            }) : node;
        }

        var isRequiredProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isRequired));
        var isAutoCompleteProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isAutoComplete));
        if (isRequiredProp is not null && isAutoCompleteProp is not null)
        {
            var newValue = new
            {
                required = Plugin.ConvertDotNetPathToJsPath(isRequiredProp.Value),

                autoComplete = isAutoCompleteProp.Value switch
                {
                    var value when "true".EqualsOrdinalIgnoreCase(value) => "'on'",

                    var value when "false".EqualsOrdinalIgnoreCase(value) => "'off'",

                    _ => $"{Plugin.ConvertDotNetPathToJsPath(isAutoCompleteProp.Value)} ? \"on\" : \"off\""
                }
            };
            
            node = node with
            {
                Properties = node.Properties.Remove(isRequiredProp).Remove(isAutoCompleteProp).Add(new()
                {
                    Name  = "valueConstraint",
                    Value = $"{{ required: {newValue.required}, autoComplete: {newValue.autoComplete} }}"
                })
            };
        }

        node = AddContextProp(node);

        return Result.From((node, new TsImportCollection
        {
            { nameof(BInput), "b-input" }
        }));
    }

    protected override Element render()
    {
        return new div(PaddingTop(16), PaddingBottom(8))
        {
            Id(id), OnClick(onMouseClick),

            new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
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