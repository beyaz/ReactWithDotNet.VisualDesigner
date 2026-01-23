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

        input = ApplyTranslateOperationOnProps(input, nameof(labelText), nameof(placeholder));

        var node = Run(input.Node,
        [
            Transforms.OnChange,
            Transforms.InputProps,
            Transforms.ValueConstraint
        ]);

        var importCollection = new TsImportCollection
        {
            { nameof(BDigitalDatepicker), "b-digital-datepicker" }
        };

        return Result.From((node, importCollection));
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
                new BSymbol { symbol = "Calendar_Month", weight = "500", color = rgb(22, 160, 133) }
            }
        };
    }

    static class Transforms
    {
        internal static ReactNode InputProps(ReactNode node)
        {
            return node.TransformIfHasProperty(nameof(placeholder), (n, prop) =>
            {
                var inputProps = new
                {
                    placeholder = IsStringValue(prop.Value) ? prop.Value : $"{Plugin.ConvertDotNetPathToJsPath(prop.Value)}"
                };

                return n with
                {
                    Properties = n.Properties.Remove(prop).Add(new()
                    {
                        Name  = "inputProps",
                        Value = $"{{ placeholder: {inputProps.placeholder} }}"
                    })
                };
            });
        }

        internal static ReactNode OnChange(ReactNode node)
        {
            if (node.Properties.HasFunctionAssignment(nameof(onDateChange)))
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
                where property.Name == nameof(onDateChange)
                let value = property.Value
                select IsAlphaNumeric(value) ? value + "(value);" : value
            };

            return
                onChangeFunctionBody.HasLine
                    ? node.UpdateProp(nameof(onDateChange), new()
                    {
                        "(value: Date) =>",
                        "{", onChangeFunctionBody, "}"
                    })
                    : node;
        }

        internal static ReactNode ValueConstraint(ReactNode node)
        {
            return node.TransformIfHasProperty(nameof(isRequired), (n, prop) =>
            {
                var required = Plugin.ConvertDotNetPathToJsPath(prop.Value);

                return n with
                {
                    Properties = n.Properties.Remove(prop).Add(new()
                    {
                        Name  = "valueConstraint",
                        Value = $"{{ required: {required} }}"
                    })
                };
            });
        }
    }
}