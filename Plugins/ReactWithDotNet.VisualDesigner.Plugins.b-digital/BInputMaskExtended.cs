namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
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

    static class Transforms
    {
        internal static ReactNode OnChange(ReactNode node)
        {
            if (node.Properties.HasFunctionAssignment(nameof(onChange)))
            {
                return node;
            }

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

            if (onChangeFunctionBody.HasLine)
            {
                return node.UpdateProp(nameof(onChange), new()
                {
                    "(e: any, value: any) =>",
                    "{", onChangeFunctionBody, "}"
                });
            }

            return node;
        }
        
        internal static ReactNode ValueConstraint(ReactNode node)
        {
            var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
            var onChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onChange));
            var isRequiredProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isRequired));
            var isAutoCompleteProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isAutoComplete));

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
            return node;
        }
    }
    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BInputMaskExtended))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        input                       = ApplyTranslateOperationOnProps(input, nameof(floatingLabelText), nameof(helperText));


        var node = input.Node;
        
        node = Run(node, [
            Transforms.OnChange,
            Transforms.ValueConstraint,
            AddContextProp
        ]);

        return Result.From((node, new TsImportCollection
        {
            {nameof(BInputMaskExtended),"b-input-mask-extended"}
        }));
    }

    protected override Element render()
    {
        var textContent = string.Empty;
        if (floatingLabelText.HasValue)
        {
            textContent = floatingLabelText;
        }

        if (value.HasValue)
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