using ReactWithDotNet.ThirdPartyLibraries.GoogleMaterialSymbols;
using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalFileInput : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string labelText { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onAddedBase64 { get; set; }
    
    [JsTypeInfo(JsType.Function)]
    public string onDeleted { get; set; }
    
    [JsTypeInfo(JsType.String)]
    public string maxFileSizeText { get; set; }
    
    [JsTypeInfo(JsType.Array)]
    public string initialFiles { get; set; }

    [Suggestions("base64")]
    [JsTypeInfo(JsType.String)]
    public string returnFormat { get; set; }
    
    [Suggestions("true")]
    [JsTypeInfo(JsType.Boolean)]
    public string isRequired { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalFileInput))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var node = input.Node;
        
        
       
        node = ApplyTranslateOperationOnProps(node, input.ComponentConfig, nameof(labelText), nameof(maxFileSizeText));
       
        var isRequiredProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isRequired));
        
        if (isRequiredProp is not null)
        {
            node = node with
            {
                Properties = node.Properties.Remove(isRequiredProp).Add(new()
                {
                    Name = "valueConstraint",
                    Value = $$"""{ required: {{Plugin.ConvertDotNetPathToJsPath(isRequiredProp.Value)}} }"""
                })
            };
        }

        
        return Result.From((node, new TsImportCollection
        {
            { nameof(BDigitalFileInput), "b-digital-file-input" }
        }));
    }

    protected override Element render()
    {
        return new FlexRowCentered(BorderRadius(10), Border(1,solid, "#16A085"), Gap(4), WidthFitContent, PaddingX(15), PaddingY(5))
        {
            new MaterialSymbol
            {
                name = "upload_file",
                size = 24,
                color = "#16A085"
            },
            labelText,
            FontSize14, FontWeight500, Color("#16A085"),
            
            Id(id), OnClick(onMouseClick)
        };
    }
}