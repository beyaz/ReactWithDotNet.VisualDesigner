namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BInputMaskExtended : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string autoComplete { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string disabled { get; set; }

    [JsTypeInfo(JsType.String)]
    public string floatingLabelText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string helperText { get; set; }


    [JsTypeInfo(JsType.Boolean)]
    public string isReadonly { get; set; }
    
    [JsTypeInfo(JsType.Array)]
    public string mask { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string maxLength { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onChange { get; set; }

    [JsTypeInfo(JsType.String)]
    public string required { get; set; }

    [JsTypeInfo(JsType.String)]
    public string value { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BInputMaskExtended))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        input = ApplyTranslateOperationOnProps(input, nameof(floatingLabelText), nameof(helperText));

        var node = input.Node;

        node = Run(node, [
            Transforms.OnChange,
            Transforms.ValueConstraint,
            AddContextProp
        ]);

        return Result.From((node, new TsImportCollection
        {
            { nameof(BInputMaskExtended), "b-input-mask-extended" }
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
            var newValue = new
            {
                required = node.FindPropByName(nameof(required))?.Value switch
                {
                    { } x => x is "true" ? x : $"{{ message: {x} }}",

                    null => null
                },

                autoComplete = node.FindPropByName(nameof(autoComplete))?.Value switch
                {
                    "true" => "'on'",

                    "false" => "'off'",

                    { } x => $"{Plugin.ConvertDotNetPathToJsPath(x)} ? 'on' : 'off'",

                    null => null
                }
            };

            var lines = new List<string>();

            if (newValue.required.HasValue)
            {
                lines.Add($"required: {newValue.required}");

                node = node.RemoveProp(nameof(required)).reactNode;
            }

            if (newValue.autoComplete.HasValue)
            {
                lines.Add($"autoComplete: {newValue.autoComplete}");

                node = node.RemoveProp(nameof(autoComplete)).reactNode;
            }

            if (lines.Count > 0)
            {
                node = node.Insert_valueConstraint(string.Join(",", lines));
            }

            return node;
        }
    }
}