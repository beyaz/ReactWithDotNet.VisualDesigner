using ReactWithDotNet.ThirdPartyLibraries.GoogleMaterialSymbols;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalFileInput : PluginComponentBase
{
    [JsTypeInfo(JsType.Array)]
    public string initialFiles { get; set; }

    [JsTypeInfo(JsType.String)]
    public string labelText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string maxFileSizeText { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onAddedBase64 { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onDeleted { get; set; }

    [JsTypeInfo(JsType.String)]
    public string required { get; set; }

    [Suggestions("base64")]
    [JsTypeInfo(JsType.String)]
    public string returnFormat { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalFileInput))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        input = ApplyTranslateOperationOnProps(input, nameof(labelText), nameof(maxFileSizeText));

        var node = input.Node;

        node = Run(node, [
            Transforms.ValueConstraint
        ]);

        return Result.From((node, new TsImportCollection
        {
            { nameof(BDigitalFileInput), "b-digital-file-input" }
        }));
    }

    protected override Element render()
    {
        return new FlexRowCentered(BorderRadius(10), Border(1, solid, "#16A085"), Gap(4), WidthFitContent, PaddingX(15), PaddingY(5))
        {
            new MaterialSymbol
            {
                name  = "upload_file",
                size  = 24,
                color = "#16A085"
            },
            labelText,
            FontSize14, FontWeight500, Color("#16A085"),

            Id(id), OnClick(onMouseClick)
        };
    }

    static class Transforms
    {
        internal static ReactNode ValueConstraint(ReactNode node)
        {
            var newValue = new
            {
                required = node.FindPropByName(nameof(required))?.Value switch
                {
                    { } x and ("true" or "false") => x,

                    { } x => $"{{ message: {x} }}",

                    null => null
                }
            };

            if (newValue.required.HasValue)
            {
                node = node.Insert_valueConstraint($"required: {newValue.required}");

                node = node.RemoveProp(nameof(required)).reactNode;
            }

            return node;
        }
    }
}